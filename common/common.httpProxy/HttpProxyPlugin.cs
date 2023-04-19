using common.proxy;

namespace common.httpProxy
{
    public interface IHttpProxyPlugin: IProxyPlugin
    {

    }

    public class HttpProxyPlugin : IHttpProxyPlugin
    {
        public virtual byte Id => config.Plugin;
        public virtual EnumProxyBufferSize BufferSize => config.BufferSize;
        public virtual ushort Port => (ushort)config.ListenPort;
        public virtual bool Enable => config.ListenEnable;

        private readonly Config config;
        private readonly IProxyServer proxyServer;
        public HttpProxyPlugin(Config config, IProxyServer proxyServer)
        {
            this.config = config;
            this.proxyServer = proxyServer;
        }

        public EnumProxyValidateDataResult ValidateData(ProxyInfo info)
        {
            return EnumProxyValidateDataResult.Equal;
        }

        public virtual bool HandleRequestData(ProxyInfo info)
        {
            

            return true;
        }

        public virtual bool ValidateAccess(ProxyInfo info)
        {
            return Enable;
        }

        public void HandleAnswerData(ProxyInfo info)
        {
            
        }

    }
}
