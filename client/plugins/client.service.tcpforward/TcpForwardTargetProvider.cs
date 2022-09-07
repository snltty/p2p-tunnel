using client.messengers.clients;
using client.messengers.register;
using common.server;
using common.tcpforward;

namespace client.service.tcpforward
{
    internal class TcpForwardTargetProvider : ITcpForwardTargetProvider
    {
        private readonly IClientInfoCaching clientInfoCaching;
        private readonly ITcpForwardTargetCaching<TcpForwardTargetCacheInfo> tcpForwardTargetCaching;
        private readonly RegisterStateInfo registerStateInfo;

        public TcpForwardTargetProvider(IClientInfoCaching clientInfoCaching, ITcpForwardTargetCaching<TcpForwardTargetCacheInfo> tcpForwardTargetCaching, RegisterStateInfo registerStateInfo)
        {
            this.clientInfoCaching = clientInfoCaching;
            this.tcpForwardTargetCaching = tcpForwardTargetCaching;
            this.registerStateInfo = registerStateInfo;
            registerStateInfo.OnRegisterStateChange.Sub((state) =>
            {
                tcpForwardTargetCaching.ClearConnection();
            });
            clientInfoCaching.OnOffline.Sub((client) =>
            {
                tcpForwardTargetCaching.ClearConnection(client.Name);
            });
        }

        public void Get(string domain, TcpForwardInfo info)
        {
            GetTarget(tcpForwardTargetCaching.Get(domain), info);
        }
        public void Get(int port, TcpForwardInfo info)
        {
            GetTarget(tcpForwardTargetCaching.Get(port), info);
        }

        private void GetTarget(TcpForwardTargetCacheInfo cacheInfo, TcpForwardInfo info)
        {
            if (cacheInfo != null)
            {
                if (cacheInfo.Connection == null || !cacheInfo.Connection.Connected)
                {
                    cacheInfo.Connection = SelectConnection(cacheInfo);
                }
                info.Connection = cacheInfo.Connection;
                info.TargetEndpoint = cacheInfo.Endpoint;
            }
        }

        private IConnection SelectConnection(TcpForwardTargetCacheInfo cacheInfo)
        {
            if (string.IsNullOrWhiteSpace(cacheInfo.Name))
            {
                return cacheInfo.TunnelType switch
                {
                    TcpForwardTunnelTypes.TCP_FIRST => registerStateInfo.TcpConnection ?? registerStateInfo.UdpConnection,
                    TcpForwardTunnelTypes.UDP_FIRST => registerStateInfo.UdpConnection ?? registerStateInfo.TcpConnection,
                    TcpForwardTunnelTypes.TCP => registerStateInfo.TcpConnection,
                    TcpForwardTunnelTypes.UDP => registerStateInfo.UdpConnection,
                    _ => registerStateInfo.OnlineConnection,
                };
            }

            ClientInfo client = clientInfoCaching.GetByName(cacheInfo.Name);
            if (client == null)
            {
                return null;
            }

            return cacheInfo.TunnelType switch
            {
                TcpForwardTunnelTypes.TCP_FIRST => client.TcpConnection ?? client.UdpConnection,
                TcpForwardTunnelTypes.UDP_FIRST => client.UdpConnection ?? client.TcpConnection,
                TcpForwardTunnelTypes.TCP => client.TcpConnection,
                TcpForwardTunnelTypes.UDP => client.UdpConnection,
                _ => client.TcpConnection,
            };
        }
    }
}