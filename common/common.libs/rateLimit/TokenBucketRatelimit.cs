using System;
using System.Collections.Concurrent;

namespace common.libs.rateLimit
{
    /// <summary>
    /// 令牌桶算法
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    public class TokenBucketRatelimit<TKey> : IRateLimit<TKey>
    {

        int ms = 10;
        float token = 0;
        int rate = 0;
        int timeout = 0;
        RateLimitTimeType type = RateLimitTimeType.Second;
        private readonly ConcurrentDictionary<TKey, TokenBucketRateInfo> limits = new ConcurrentDictionary<TKey, TokenBucketRateInfo>();
        private readonly WheelTimer<TKey> wheelTimer = new WheelTimer<TKey>();

        public TokenBucketRatelimit()
        {

        }

        public void Init(int rate, RateLimitTimeType type)
        {
            this.rate = rate;
            this.type = type;
            (ms, token, timeout) = GetMsAndToken(rate, type);
        }

        public void SetRate(TKey key, int rate)
        {
            (_, float token, int timeout) = GetMsAndToken(rate, type);

            if (limits.TryGetValue(key, out TokenBucketRateInfo info) == false)
            {
                info = new TokenBucketRateInfo { Rate = rate, CurrentRate = rate, Token = rate, Ms = ms, LastTime = DateTimeHelper.GetTimeStamp(), TimeoutMs = timeout };
                limits.TryAdd(key, info);
            }
            else
            {
                info.Rate = rate;
                info.CurrentRate = rate;
                info.Token = token;
                info.Timeout.Cancel();
            }
            info.Timeout = wheelTimer.NewTimeout(new WheelTimerTimeoutTask<TKey> { State = key, Callback = Timeout }, 1000, true);
        }
        public int Try(TKey key, int num)
        {
            TokenBucketRateInfo info = Get(key);
            if (info.Rate == 0)
            {
                return num;
            }
            AddToken(info);
            //消耗掉能消耗的
            int canEat = Math.Min(num, (int)info.CurrentRate);
            info.CurrentRate -= canEat;
            return canEat;
        }
        public void Remove(TKey key)
        {
            if (limits.TryRemove(key, out TokenBucketRateInfo info))
            {
                info.Timeout.Cancel();
            };
        }
        public void Disponse()
        {
            foreach (var item in limits.Values)
            {
                item.Timeout.Cancel();
            }
            limits.Clear();
        }
        private TokenBucketRateInfo Get(TKey key)
        {
            if (limits.TryGetValue(key, out TokenBucketRateInfo info) == false)
            {
                info = new TokenBucketRateInfo { Rate = rate, CurrentRate = rate, Token = token, Ms = ms, LastTime = DateTimeHelper.GetTimeStamp(), TimeoutMs = timeout };
                limits.TryAdd(key, info);
                info.Timeout = wheelTimer.NewTimeout(new WheelTimerTimeoutTask<TKey> { State = key, Callback = Timeout }, 1000, true);
            }
            return info;
        }


        private void Timeout(WheelTimerTimeout<TKey> timeout)
        {
            if (limits.TryGetValue(timeout.Task.State, out TokenBucketRateInfo info))
            {
                if (DateTimeHelper.GetTimeStamp() - info.LastTime > info.TimeoutMs)
                {
                    info.Timeout.Cancel();
                    limits.TryRemove(timeout.Task.State, out _);
                }
            }
        }
        private void AddToken(TokenBucketRateInfo info)
        {
            long time = DateTimeHelper.GetTimeStamp();
            int times = (int)(time - info.LastTime) / info.Ms;

            info.CurrentRate = Math.Min(info.CurrentRate + times * info.Token, info.Rate);

            info.LastTime += info.Ms * times;
        }
        private (int, float, int) GetMsAndToken(int rate, RateLimitTimeType type)
        {
            float token = 0;
            int ms = 0, timeout = 0;
            switch (type)
            {
                case RateLimitTimeType.Second:
                    ms = this.ms;
                    token = rate / (1000 / this.ms);
                    timeout = 1000 * 2;
                    break;
                case RateLimitTimeType.Minute:
                    ms = 1000;
                    token = rate / 60.0f;
                    timeout = 1000 * 60 * 2;
                    break;
                case RateLimitTimeType.Hour:
                    ms = 1000 * 60;
                    token = rate / 60.0f;
                    timeout = 1000 * 60 * 60 * 2;
                    break;
                case RateLimitTimeType.Day:
                    ms = 1000 * 60 * 60;
                    token = rate / 24.0f;
                    timeout = 1000 * 60 * 60 * 24 * 2;
                    break;
                default:
                    break;
            }
            return (ms, token, timeout);
        }


        class TokenBucketRateInfo
        {
            public float Rate { get; set; }
            public float CurrentRate { get; set; }
            public float Token { get; set; }

            public int Ms { get; set; }
            public int TimeoutMs { get; set; }
            public WheelTimerTimeout<TKey> Timeout { get; set; }
            public long LastTime { get; set; }

        }
    }
}
