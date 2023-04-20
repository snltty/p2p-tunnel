using common.proxy;
using common.libs.extends;
using common.socks5;
using server.messengers.singnin;
using server.messengers;

namespace server.service.socks5
{
    public interface IServerSocks5ProxyPlugin : ISocks5ProxyPlugin
    {

    }

    public class ServerSocks5ProxyPlugin : Socks5ProxyPlugin, IServerSocks5ProxyPlugin
    {
        public static uint Access => 0b00000000_00000000_00000000_00010000;
        private readonly IServiceAccessValidator serviceAccessProvider;
        private readonly IClientSignInCaching clientSignInCaching;
        public ServerSocks5ProxyPlugin(common.socks5.Config config, IProxyServer proxyServer, ISocks5ConnectionProvider socks5ConnectionProvider
            , IServiceAccessValidator serviceAccessProvider, IClientSignInCaching clientSignInCaching)
            : base(config, proxyServer, socks5ConnectionProvider)
        {
            this.serviceAccessProvider = serviceAccessProvider;
            this.clientSignInCaching = clientSignInCaching;
        }

        public override bool ValidateAccess(ProxyInfo info)
        {
            if (info.TargetAddress.IsLan())
            {
                return false;
            }

            if (clientSignInCaching.Get(info.Connection.ConnectId, out SignInCacheInfo client))
            {
                return Enable || serviceAccessProvider.Validate(client.UserAccess, Access);
            }

            return Enable;
        }
    }
}
