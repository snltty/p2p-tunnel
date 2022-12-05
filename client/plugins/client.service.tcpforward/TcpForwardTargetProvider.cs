using client.messengers.clients;
using client.messengers.register;
using common.server;
using common.tcpforward;

namespace client.service.tcpforward
{
    /// <summary>
    /// tcp转发目标提供
    /// </summary>
    internal sealed class TcpForwardTargetProvider : ITcpForwardTargetProvider
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

        /// <summary>
        /// 根据host获取目标连接
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="info"></param>
        public void Get(string domain, TcpForwardInfo info)
        {
            GetTarget(tcpForwardTargetCaching.Get(domain), info);
        }
        /// <summary>
        /// 根据端口获取目标连接
        /// </summary>
        /// <param name="port"></param>
        /// <param name="info"></param>
        public void Get(ushort port, TcpForwardInfo info)
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
                return registerStateInfo.OnlineConnection;
            }

            if (clientInfoCaching.GetByName(cacheInfo.Name, out ClientInfo client))
            {
                return client.Connection;
            }
            return null;
        }
    }
}