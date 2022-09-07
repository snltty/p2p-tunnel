using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace common.libs.rateLimit
{
    public class SlidingWindowRateLimit<TKey> : IRateLimit<TKey>
    {
        private readonly ConcurrentDictionary<TKey, SlidingRateInfo> limits = new ConcurrentDictionary<TKey, SlidingRateInfo>();
        private int rate = 0;
        private bool disponsed = true;
        private int windowLength = 0;
        private Func<long> timeFunc;

        public void Init(int rate, RateLimitTimeType type)
        {
            this.rate = rate;
            disponsed = false;

            switch (type)
            {
                case RateLimitTimeType.Second:
                    windowLength = 20;
                    timeFunc = DateTimeHelper.GetTimeStamp;
                    break;
                case RateLimitTimeType.Minute:
                    windowLength = 60;
                    timeFunc = DateTimeHelper.GetTimeStampSec;
                    break;
                case RateLimitTimeType.Hour:
                    windowLength = 60;
                    timeFunc = DateTimeHelper.GetTimeStampMinute;
                    break;
                case RateLimitTimeType.Day:
                    windowLength = 24;
                    timeFunc = DateTimeHelper.GetTimeStampHour;
                    break;
                default:
                    break;
            }
        }

        public void SetRate(TKey key, int num)
        {
            if (disponsed == false)
            {
                if (limits.TryGetValue(key, out SlidingRateInfo info) == false)
                {
                    info = new SlidingRateInfo { Rate = num, Items = new SlidingRateItemInfo[windowLength], CurrentRate = 0 };
                    limits.TryAdd(key, info);
                }
                else
                {
                    info.Rate = num;
                }
            }
        }

        public async Task<bool> TryGet(TKey key, int num, bool wait)
        {
            if (disponsed == false)
            {
                long time = timeFunc();
                if (limits.TryGetValue(key, out SlidingRateInfo info) == false)
                {
                    info = new SlidingRateInfo { Rate = rate, Items = new SlidingRateItemInfo[windowLength], CurrentRate = 0 };
                    info.Items[0] = new SlidingRateItemInfo { Time = time };
                    limits.TryAdd(key, info);
                }

                int last = num;
                do
                {
                    Monitor.Enter(this);
                    try
                    {
                        time = timeFunc();

                        int index = (int)(time - info.Items[0].Time) / windowLength;
                        if (index > 0)
                        {
                            if (index > windowLength)
                            {
                                info.CurrentRate = 0;
                                Array.Clear(info.Items, 0, info.Items.Length);
                            }
                            else
                            {
                                for (int i = index; i < info.Items.Length; i++)
                                {
                                    if (info.Items[i] == null)
                                    {
                                        break;
                                    }
                                    info.CurrentRate -= info.Items[i].Rate;
                                }
                                info.Items.AsSpan(0, info.Items.Length - index).CopyTo(info.Items.AsSpan(index));
                            }
                            info.Items[0] = new SlidingRateItemInfo { Time = time };
                        }

                        //消耗掉能消耗的
                        int canAdd = info.Rate - info.CurrentRate;
                        last -= canAdd;
                        info.CurrentRate += canAdd;
                        info.Items[0].Rate += canAdd;

                        Monitor.Exit(this);
                        if (last > 0)
                        {
                            await Task.Delay(30);
                        }
                    }
                    catch (Exception ex)
                    {
                        Monitor.Exit(this);
                        Logger.Instance.Error(ex);
                    }
                } while (last > 0);

            }
            return true;
        }

        public void Disponse()
        {
            limits.Clear();
            disponsed = true;
        }

        class SlidingRateInfo
        {
            public int Rate { get; set; }
            public int CurrentRate { get; set; }
            public SlidingRateItemInfo[] Items { get; init; }
        }
        class SlidingRateItemInfo
        {
            public long Time { get; set; }
            public int Rate { get; set; } = 0;
        }
    }
}
