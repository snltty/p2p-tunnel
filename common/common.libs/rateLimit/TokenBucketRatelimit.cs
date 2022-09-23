using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace common.libs.rateLimit
{
    /// <summary>
    /// 令牌桶算法
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    public class TokenBucketRatelimit<TKey> : IRateLimit<TKey>
    {
        int ms = 30;
        int token = 0;
        int rate = 0;
        RateLimitTimeType type = RateLimitTimeType.Second;
        WheelTimer<TokenBucketRateInfo> wheelTimer = new WheelTimer<TokenBucketRateInfo>();
        private readonly ConcurrentDictionary<TKey, TokenBucketRateInfo> limits = new ConcurrentDictionary<TKey, TokenBucketRateInfo>();

        public void Init(int rate, RateLimitTimeType type)
        {
            this.rate = rate;
            this.type = type;
            (ms, token) = GetMsAndToken(rate, type);
        }
        private (int, int) GetMsAndToken(int rate, RateLimitTimeType type)
        {
            int token = 0, ms = 0;
            switch (type)
            {
                case RateLimitTimeType.Second:
                    token = rate / (1000 / ms);
                    break;
                case RateLimitTimeType.Minute:
                    ms = 1000;
                    token = rate / 60;
                    break;
                case RateLimitTimeType.Hour:
                    ms = 1000 * 60;
                    token = rate / 60;
                    break;
                case RateLimitTimeType.Day:
                    ms = 1000 * 60 * 60;
                    token = rate / 24;
                    break;
                default:
                    break;
            }
            return (ms, token);
        }
        private void Callback(WheelTimerTimeout<TokenBucketRateInfo> timeout)
        {
            if(timeout.Task.State.CurrentRate < timeout.Task.State.Rate)
            {
                timeout.Task.State.CurrentRate += timeout.Task.State.Token;
            }
        }

        public void SetRate(TKey key, int rate)
        {
            Monitor.Enter(this);
            (int ms, int token) = GetMsAndToken(rate, type);

            if (limits.TryGetValue(key, out TokenBucketRateInfo info) == false)
            {
                info = new TokenBucketRateInfo { Rate = rate, Token = token };
                limits.TryAdd(key, info);
            }
            else
            {
                info.Rate = rate;
                info.Token = token;
                info.Timeout.Cancel();
            }
            info.Timeout = wheelTimer.NewTimeout(new WheelTimerTimeoutTask<TokenBucketRateInfo>
            {
                State = info,
                Callback = Callback
            }, ms, true);
            Monitor.Exit(this);
        }

        public bool Try(TKey key, int num)
        {
            try
            {
                Monitor.Enter(this);
                TokenBucketRateInfo info = Get(key);
                bool res = info.CurrentRate + num <= info.Rate;
                if (res)
                {
                    info.CurrentRate += num;
                }
                return res;
            }
            catch (Exception)
            {
            }
            finally
            {
                Monitor.Exit(this);
            }
            return false;
        }

        public async Task TryWait(TKey key, int num)
        {
            int last = num;
            do
            {
                try
                {
                    Monitor.Enter(this);
                    TokenBucketRateInfo info = Get(key);

                    //消耗掉能消耗的
                    int canEat = Math.Min(last, info.CurrentRate);
                    last -= canEat;
                    info.CurrentRate -= canEat;

                    Monitor.Exit(this);
                    if (last > 0)
                    {
                        await Task.Delay(15);
                    }
                }
                catch (Exception)
                {
                    Monitor.Exit(this);
                }
            } while (last > 0);
        }
        private TokenBucketRateInfo Get(TKey key)
        {
            if (limits.TryGetValue(key, out TokenBucketRateInfo info) == false)
            {
                info = new TokenBucketRateInfo { Rate = rate, Token = token };
                info.Timeout = wheelTimer.NewTimeout(new WheelTimerTimeoutTask<TokenBucketRateInfo>
                {
                    State = info,
                    Callback = Callback
                }, ms, true);
                limits.TryAdd(key, info);
            }
            return info;
        }

        public void Remove(TKey key)
        {
            if (limits.TryRemove(key, out TokenBucketRateInfo info))
            {
                info.Timeout.Cancel();
            }
        }

        public void Disponse()
        {
            foreach (var item in limits.Values)
            {
                item.Timeout.Cancel();
            }
            limits.Clear();
        }


        class TokenBucketRateInfo
        {
            public int Rate { get; set; }
            public int CurrentRate { get; set; }
            public int Token { get; set; }
            public WheelTimerTimeout<TokenBucketRateInfo> Timeout { get; set; }
        }
    }
}
