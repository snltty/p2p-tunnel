using common.proxy;
using common.socks5;
using server.messengers;
using common.libs.extends;
using common.server;

namespace server.service.socks5
{
    public interface IServerSocks5ProxyPlugin : ISocks5ProxyPlugin
    {

    }

    public class ServerSocks5ProxyPlugin : Socks5ProxyPlugin, IServerSocks5ProxyPlugin
    {
        private readonly IServiceAccessValidator  serviceAccessValidator;
        public ServerSocks5ProxyPlugin(common.socks5.Config config, IProxyServer proxyServer, IServiceAccessValidator serviceAccessValidator)
            : base(config, proxyServer, serviceAccessValidator)
        {
            this.serviceAccessValidator = serviceAccessValidator;
        }

        public override bool ValidateAccess(ProxyInfo info)
        {
           
#if DEBUG
            return true;
#else
            return base.ValidateAccess(info) || serviceAccessValidator.Validate(info.Connection, Access);
#endif
        }
    }
}
