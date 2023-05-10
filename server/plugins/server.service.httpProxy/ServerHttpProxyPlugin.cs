using common.httpProxy;
using common.libs.extends;
using common.proxy;
using common.server;
using server.messengers;

namespace server.service.httpProxy
{
    public interface IServerHttpProxyPlugin : IHttpProxyPlugin
    {
    }

    public sealed class ServerHttpProxyPlugin : HttpProxyPlugin, IServerHttpProxyPlugin
    {
        private readonly IServiceAccessValidator serviceAccessProvider;
        public ServerHttpProxyPlugin(common.httpProxy.Config config, IServiceAccessValidator serviceAccessProvider) : base(config, serviceAccessProvider)
        {
            this.serviceAccessProvider = serviceAccessProvider;
        }

        public override bool ValidateAccess(ProxyInfo info)
        {
#if DEBUG
            return true;
#else
            return base.ValidateAccess(info) || serviceAccessProvider.Validate(info.Connection, Access);
#endif
        }
    }
}
