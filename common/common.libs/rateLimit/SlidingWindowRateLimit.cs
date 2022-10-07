using System;
using System.Collections.Concurrent;

namespace common.libs.rateLimit
{
    public class SlidingWindowRateLimit<TKey> : IRateLimit<TKey>
    {
        private readonly ConcurrentDictionary<TKey, SlidingRateInfo> limits = new ConcurrentDictionary<TKey, SlidingRateInfo>();
        private int rate = 0;
        private int windowLength = 20;
        private int mask = 1000 / 20;
        private Func<long> timeFunc = DateTimeHelper.GetTimeStamp;

        public SlidingWindowRateLimit(int rate)
        {
            this.rate = rate;
        }

        public void SetRate(TKey key, int rate)
        {
            if (limits.TryGetValue(key, out SlidingRateInfo info) == false)
            {
                info = new SlidingRateInfo { Rate = rate, Items = new SlidingRateItemInfo[windowLength], CurrentRate = 0 };
                limits.TryAdd(key, info);
            }
            else
            {
                info.Rate = rate;
            }
        }

        public int Try(TKey key, int num)
        {
            SlidingRateInfo info = Get(key);
            Move(info);

            //消耗掉能消耗的
            int canEat = info.Rate - info.CurrentRate;
            if (num < canEat)
            {
                canEat = num;
            }

            info.CurrentRate += canEat;
            info.Items[0].Rate += canEat;

            return canEat;
        }

        private void Move(SlidingRateInfo info)
        {
            long time = timeFunc();
            int index = (int)(time - info.Items[0].Time) / mask;
            if (index > 0)
            {
                info.CurrentRate = 0;
                if (index > windowLength)
                {
                    Array.Clear(info.Items, 0, info.Items.Length);
                }
                else
                {
                    info.Items.AsSpan(0, info.Items.Length - index).CopyTo(info.Items.AsSpan(index));
                    Array.Clear(info.Items, 0, index);
                }
                info.Items[0] = new SlidingRateItemInfo { Time = time };
                for (int i = 0; i < info.Items.Length; i++)
                {
                    if (info.Items[i] != null)
                    {
                        info.CurrentRate += info.Items[i].Rate;
                    }
                }
            }
        }
        private SlidingRateInfo Get(TKey key)
        {
            if (limits.TryGetValue(key, out SlidingRateInfo info) == false)
            {
                info = new SlidingRateInfo { Rate = rate, Items = new SlidingRateItemInfo[windowLength], CurrentRate = 0 };
                info.Items[0] = new SlidingRateItemInfo { Time = timeFunc() };
                limits.TryAdd(key, info);
            }
            return info;
        }

        public void Remove(TKey key)
        {
            limits.TryRemove(key, out _);
        }
        public void Clear()
        {
            limits.Clear();
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
