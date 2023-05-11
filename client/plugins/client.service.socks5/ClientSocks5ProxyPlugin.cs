using client.messengers.clients;
using client.messengers.singnin;
using common.proxy;
using common.server;
using common.socks5;

namespace client.service.socks5
{
    public interface IClientSocks5ProxyPlugin : ISocks5ProxyPlugin
    {

    }

    public class ClientSocks5ProxyPlugin : Socks5ProxyPlugin, IClientSocks5ProxyPlugin
    {
        private readonly common.socks5.Config config;
        private readonly SignInStateInfo signInStateInfo;
        private readonly IClientInfoCaching clientInfoCaching;
        public ClientSocks5ProxyPlugin(common.socks5.Config config, SignInStateInfo signInStateInfo, IClientInfoCaching clientInfoCaching, IProxyServer proxyServer, IServiceAccessValidator serviceAccessValidator)
            : base(config, proxyServer, serviceAccessValidator)
        {
            this.config = config;
            this.signInStateInfo = signInStateInfo;
            this.clientInfoCaching = clientInfoCaching;
        }

        public override bool HandleRequestData(ProxyInfo info)
        {
            bool res = base.HandleRequestData(info);
            if (res == false)
            {
                return res;
            }

            GetConnection(info);
            return true;
        }


        ulong target = 0;
        private void GetConnection(ProxyInfo info)
        {
            if (info.Connection == null || info.Connection.Connected == false || target != config.TargetConnectionId)
            {
                target = config.TargetConnectionId;
                if (config.TargetConnectionId == 0)
                {
                    info.Connection = signInStateInfo.Connection;
                }
                else
                {
                    if (clientInfoCaching.Get(config.TargetConnectionId, out ClientInfo client))
                    {
                        info.Connection = client.Connection;
                    }
                }
            }
        }
    }
}
