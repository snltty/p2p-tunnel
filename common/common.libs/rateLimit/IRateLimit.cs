namespace common.libs.rateLimit
{
    public interface IRateLimit<TKey>
    {
        /// <summary>
        /// 单独设置某个对象的速率
        /// </summary>
        /// <param name="key"></param>
        /// <param name="num"></param>
        void SetRate(TKey key, int rate);

        /// <summary>
        /// 检查一下是否可通行
        /// </summary>
        /// <param name="key">对象key</param>
        /// <param name="num">本次输入的值</param>
        /// <returns>消耗值，0则表示未能消耗任何流量</returns>
        int Try(TKey key, int num);

        /// <summary>
        /// 移除
        /// </summary>
        /// <param name="key"></param>
        void Remove(TKey key);

        /// <summary>
        /// 清理，清理后需init后才能再次使用
        /// </summary>
        void Clear();
    }
}
