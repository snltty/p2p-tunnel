using client.messengers.clients;
using client.messengers.singnin;
using common.proxy;
using common.socks5;

namespace server.service.socks5
{
    public interface IClientSocks5ProxyPlugin : ISocks5ProxyPlugin
    {

    }

    public class ClientSocks5ProxyPlugin : Socks5ProxyPlugin, IClientSocks5ProxyPlugin
    {
        private readonly common.socks5.Config config;
        private readonly SignInStateInfo signInStateInfo;
        private readonly IClientInfoCaching clientInfoCaching;
        public ClientSocks5ProxyPlugin(common.socks5.Config config, SignInStateInfo signInStateInfo, IClientInfoCaching clientInfoCaching, IProxyServer proxyServer)
            : base(config, proxyServer)
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


        string targetName = string.Empty;
        private void GetConnection(ProxyInfo info)
        {
            if (info.Connection == null || info.Connection.Connected == false || targetName != config.TargetName)
            {
                targetName = config.TargetName;
                if (config.TargetName == "/")
                {
                    info.Connection = signInStateInfo.Connection;
                }
                else
                {
                    if (clientInfoCaching.GetByName(config.TargetName, out ClientInfo client))
                    {
                        info.Connection = client.Connection;
                    }
                }
            }
        }
    }
}
