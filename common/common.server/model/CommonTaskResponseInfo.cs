namespace common.server.model
{
    public class CommonTaskResponseInfo<T>
    {
        public string ErrorMsg { get; set; } = string.Empty;
        public T Data { get; set; } = default;
    }
}
