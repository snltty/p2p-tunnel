using System.Threading.Tasks;

namespace common.libs.rateLimit
{
    public interface IRateLimit<TKey>
    {
        /// <summary>
        /// 初始化后才能用
        /// </summary>
        /// <param name="rate">速率</param>
        /// <param name="type">类型</param>
        void Init(int rate, RateLimitTimeType type);

        /// <summary>
        /// 单独设置某个对象的速率
        /// </summary>
        /// <param name="key"></param>
        /// <param name="num"></param>
        void SetRate(TKey key, int num);

        /// <summary>
        /// 检查一下是否可通行
        /// </summary>
        /// <param name="key">对象key</param>
        /// <param name="num">本次输入的值</param>
        /// <param name="wait">不可通行时是否等待</param>
        /// <returns>true可通行，false不可通行</returns>
        Task<bool> TryGet(TKey key, int num, bool wait);

        /// <summary>
        /// 清理，清理后需init后才能再次使用
        /// </summary>
        void Disponse();
    }

    public enum RateLimitTimeType : byte
    {
        /// <summary>
        /// 每秒，20份，每份50ms
        /// </summary>
        Second = 0,
        /// <summary>
        /// 每分钟，60份，每份6s
        /// </summary>
        Minute = 1,
        /// <summary>
        /// 每小时，60份，每份1分钟
        /// </summary>
        Hour = 2,
        /// <summary>
        /// 每天，24份，每份1小时
        /// </summary>
        Day = 3
    }
}
