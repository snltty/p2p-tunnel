using common.proxy;
using common.socks5;

namespace server.service.socks5
{
    public interface IClientSocks5ProxyPlugin : ISocks5ProxyPlugin
    {

    }

    public class ClientSocks5ProxyPlugin : Socks5ProxyPlugin, IClientSocks5ProxyPlugin
    {
        public ClientSocks5ProxyPlugin(common.socks5.Config config, IProxyServer proxyServer, ISocks5ConnectionProvider socks5ConnectionProvider)
            : base(config, proxyServer, socks5ConnectionProvider)
        {

        }

    }
}
