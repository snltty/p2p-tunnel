using common.proxy;
using common.socks5;
using common.server;

namespace server.service.socks5
{
    public interface IServerSocks5ProxyPlugin : ISocks5ProxyPlugin
    {

    }

    public class ServerSocks5ProxyPlugin : Socks5ProxyPlugin, IServerSocks5ProxyPlugin
    {
        public ServerSocks5ProxyPlugin(common.socks5.Config config, IProxyServer proxyServer)
            : base(config, proxyServer)
        {
        }
    }
}
