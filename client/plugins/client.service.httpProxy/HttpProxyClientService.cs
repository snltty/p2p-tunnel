using client.service.ui.api.clientServer;

namespace client.service.httpProxy
{
    /// <summary>
    /// http代理
    /// </summary>
    public sealed class HttpProxyClientService : IClientService
    {
        private readonly HttpProxyTransfer httpProxyTransfer;
        public HttpProxyClientService(HttpProxyTransfer httpProxyTransfer)
        {
            this.httpProxyTransfer = httpProxyTransfer;
        }
        public bool Update(ClientServiceParamsInfo arg)
        {
           return httpProxyTransfer.Update();
        }

        public string GetPac(ClientServiceParamsInfo arg)
        {
            return httpProxyTransfer.GetPac();
        }

        public string SetPac(ClientServiceParamsInfo arg)
        {
            return httpProxyTransfer.UpdatePac(arg.Content);
        }
    }
}
