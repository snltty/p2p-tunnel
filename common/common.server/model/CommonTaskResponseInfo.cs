namespace common.server.model
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class CommonTaskResponseInfo<T>
    {
        /// <summary>
        /// 
        /// </summary>
        public string ErrorMsg { get; set; } = string.Empty;
        /// <summary>
        /// 
        /// </summary>
        public T Data { get; set; } = default;
    }
}
