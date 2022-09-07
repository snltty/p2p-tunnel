namespace client.service.ui.api.clientServer
{
    public interface IClientService { }

    public class ClientServiceResponseInfo
    {
        public string Path { get; set; } = string.Empty;
        public long RequestId { get; set; } = 0;
        public ClientServiceResponseCodes Code { get; set; } = ClientServiceResponseCodes.Success;
        public object Content { get; set; } = string.Empty;
    }

    public class ClientServiceRequestInfo
    {
        public string Path { get; set; } = string.Empty;
        public long RequestId { get; set; } = 0;
        public string Content { get; set; } = string.Empty;
    }

    public class ClientServiceParamsInfo
    {
        public long RequestId { get; set; } = 0;
        public string Content { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;

        public ClientServiceResponseCodes Code { get; private set; } = ClientServiceResponseCodes.Success;
        public string ErrorMessage { get; private set; } = string.Empty;

        public void SetCode(ClientServiceResponseCodes code, string errormsg = "")
        {
            Code = code;
            ErrorMessage = errormsg;
        }
        public void SetErrorMessage(string msg)
        {
            Code = ClientServiceResponseCodes.Error;
            ErrorMessage = msg;
        }
    }

    public enum ClientServiceResponseCodes : byte
    {
        Success = 0,
        Error = 0xff,

    }
}
