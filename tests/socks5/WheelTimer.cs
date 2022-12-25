using System.Collections.Concurrent;

namespace socks5
{
    /// <summary>
    /// 时间轮延时任务
    /// </summary>
    public sealed class WheelTimer<T>
    {
        //流转次数，
        long ticks = 0;
        //槽数
        int bucketLength = 2 << 8;
        //mask = bucketLength-1;ticks & mask; 获得槽下标
        int mask = 0;
        //精度，应使用Thread.Sleep(1)时间的倍数
        int tickDurationMs = 30;
        //槽
        WheelTimerBucket<T>[] buckets = Array.Empty<WheelTimerBucket<T>>();
        //先入列，等待入槽
        ConcurrentQueue<WheelTimerTimeout<T>> timeouts = new ConcurrentQueue<WheelTimerTimeout<T>>();
        AutoResetEvent autoReset = new AutoResetEvent(true);

        /// <summary>
        /// 
        /// </summary>
        public WheelTimer()
        {
            CreateBuckets();
            Worker();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="task"></param>
        /// <param name="delayMs"></param>
        /// <param name="reuse"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public WheelTimerTimeout<T> NewTimeout(WheelTimerTimeoutTask<T> task, int delayMs, bool reuse = false)
        {
            if (task == null)
            {
                throw new ArgumentNullException("task must be not null");
            }
            if (delayMs <= 1)
            {
                throw new ArgumentNullException("delayMs must be > 1 ms");
            }

            WheelTimerTimeout<T> timeout = new WheelTimerTimeout<T> { Delay = delayMs, Task = task, Reuse = reuse };
            timeouts.Enqueue(timeout);
            return timeout;
        }

        private void CreateBuckets()
        {
            mask = bucketLength - 1;
            buckets = new WheelTimerBucket<T>[bucketLength];
            for (int i = 0; i < buckets.Length; i++)
            {
                buckets[i] = new WheelTimerBucket<T>();
            }
        }

        double ticksMore = 0;
        private void Worker()
        {
            new Thread(() =>
            {
                for (; ; )
                {
                    long start = DateTime.UtcNow.Ticks;
                    //等下一个时间点
                    WaitForNextTick();
                    //待入槽队列入槽
                    TransferTimeoutsToBuckets();
                    //执行当前槽的任务
                    ExpireTimeouts(buckets[(ticks & mask)]);
                    ticks++;
                    ticksMore += (DateTime.UtcNow.Ticks - start) / TimeSpan.TicksPerMillisecond - tickDurationMs;

                    double forwardCount = (ticksMore / tickDurationMs);
                    while (forwardCount > 1)
                    {
                        ticksMore -= tickDurationMs;

                        start = DateTime.UtcNow.Ticks;
                        //待入槽队列入槽
                        TransferTimeoutsToBuckets();
                        //执行当前槽的任务
                        ExpireTimeouts(buckets[(ticks & mask)]);
                        ticks++;

                        ticksMore += (DateTime.UtcNow.Ticks - start) / TimeSpan.TicksPerMillisecond;

                        forwardCount = ticksMore / tickDurationMs;
                    }
                }

            })
            { IsBackground = true }.Start();
        }
        private void WaitForNextTick()
        {
            autoReset.WaitOne(tickDurationMs);
        }
        private void TransferTimeoutsToBuckets()
        {
            if (timeouts.Count == 0)
            {
                return;
            }
            //一次最多转移100000个
            for (int i = 0; i < 100000; i++)
            {
                if (timeouts.TryDequeue(out WheelTimerTimeout<T> timeout) == false)
                {
                    break;
                }
                if (timeout.IsCanceled)
                {
                    continue;
                }
                //所需格子数
                int expireTicks = (int)((timeout.Delay + ticksMore) / tickDurationMs);
                //所需轮次
                timeout.Rounds = expireTicks / buckets.Length;
                //除轮次外，剩下的格子数应在哪个槽中
                int stopIndex = ((int)(ticks & mask) + (expireTicks - timeout.Rounds * buckets.Length)) & mask;

                buckets[stopIndex].AddTimeout(timeout);
            }
        }
        private void ExpireTimeouts(WheelTimerBucket<T> bucket)
        {
            LinkedListNode<WheelTimerTimeout<T>> timeout = bucket.List.First;
            while (timeout != null)
            {
                bool remove = false;
                if (timeout.Value.Rounds <= 0 && timeout.Value.IsCanceled == false)
                {
                    remove = true;
                    try
                    {
                        timeout.Value.Task.Callback?.Invoke(timeout.Value);
                    }
                    catch (Exception) { }
                }
                else if (timeout.Value.IsCanceled)
                {
                    remove = true;
                }
                else
                {
                    timeout.Value.Rounds--;
                }

                LinkedListNode<WheelTimerTimeout<T>> next = timeout.Next;
                if (remove)
                {
                    bucket.Remove(timeout);
                    if (timeout.Value.Reuse && !timeout.Value.IsCanceled)
                    {
                        timeouts.Enqueue(timeout.Value);
                    }
                }
                timeout = next;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class WheelTimerBucket<T>
    {
        /// <summary>
        /// 
        /// </summary>
        public LinkedList<WheelTimerTimeout<T>> List { get; private set; } = new LinkedList<WheelTimerTimeout<T>>();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeout"></param>
        public void AddTimeout(WheelTimerTimeout<T> timeout)
        {
            List.AddLast(timeout);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        public void Remove(LinkedListNode<WheelTimerTimeout<T>> node)
        {
            List.Remove(node);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class WheelTimerTimeout<T>
    {
        /// <summary>
        /// 
        /// </summary>
        public int Delay { get; init; } = 0;
        /// <summary>
        /// 
        /// </summary>
        public int Rounds { get; set; } = 0;
        /// <summary>
        /// 
        /// </summary>
        public bool Reuse { get; init; } = false;
        /// <summary>
        /// 
        /// </summary>
        public WheelTimerTimeoutTask<T> Task { get; init; }
        /// <summary>
        /// 
        /// </summary>
        public TimeoutState State { get; private set; } = TimeoutState.Normal;
        /// <summary>
        /// 
        /// </summary>
        public bool IsCanceled => State == TimeoutState.Canceld;
        /// <summary>
        /// 
        /// </summary>
        public void Cancel()
        {
            State = TimeoutState.Canceld;
        }

        /// <summary>
        /// 
        /// </summary>
        public enum TimeoutState
        {
            /// <summary>
            /// 
            /// </summary>
            Normal = 1 << 0,
            /// <summary>
            /// 
            /// </summary>
            Canceld = 1 << 1,
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class WheelTimerTimeoutTask<T>
    {
        /// <summary>
        /// 保存状态数据
        /// </summary>
        public T State { get; init; }
        /// <summary>
        /// 不要抛异常影响轮转时间
        /// </summary>
        public Action<WheelTimerTimeout<T>> Callback { get; init; }
    }
}
