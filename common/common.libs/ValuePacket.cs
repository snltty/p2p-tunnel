namespace common.libs
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ValuePacket<T> where T : struct
    {
        /// <summary>
        /// 
        /// </summary>
        public T Value { get; set; }
    }
}
