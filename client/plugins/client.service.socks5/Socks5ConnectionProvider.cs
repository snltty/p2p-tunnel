using client.messengers.clients;
using client.messengers.singnin;
using common.proxy;
using common.socks5;

namespace client.service.socks5
{
    public sealed class Socks5ConnectionProvider : ISocks5ConnectionProvider
    {
        private readonly common.socks5.Config config;
        private readonly SignInStateInfo signInStateInfo;
        private readonly IClientInfoCaching clientInfoCaching;


        private string targetName;

        public Socks5ConnectionProvider(common.socks5.Config config, SignInStateInfo signInStateInfo, IClientInfoCaching clientInfoCaching)
        {
            this.config = config;
            this.signInStateInfo = signInStateInfo;
            this.clientInfoCaching = clientInfoCaching;
        }

        public void Get(ProxyInfo info)
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
