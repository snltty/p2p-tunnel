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
        public static uint Access => 0b00000000_00000000_00000000_00010000;
        private readonly IServiceAccessValidator serviceAccessProvider;
        public ServerSocks5ProxyPlugin(common.socks5.Config config, IProxyServer proxyServer, IServiceAccessValidator serviceAccessProvider)
            : base(config, proxyServer)
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
