using common.httpProxy;
using common.server;

namespace server.service.httpProxy
{
    public interface IServerHttpProxyPlugin : IHttpProxyPlugin
    {
    }

    public sealed class ServerHttpProxyPlugin : HttpProxyPlugin, IServerHttpProxyPlugin
    {
        public ServerHttpProxyPlugin(common.httpProxy.Config config, IServiceAccessValidator serviceAccessProvider) : base(config, serviceAccessProvider)
        {
        }

    }
}
