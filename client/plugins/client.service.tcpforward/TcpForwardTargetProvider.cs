using client.messengers.clients;
using client.messengers.singnin;
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
        private readonly SignInStateInfo signInStateInfo;
        private readonly IClientsTransfer clientsTransfer;

        public TcpForwardTargetProvider(IClientInfoCaching clientInfoCaching, ITcpForwardTargetCaching<TcpForwardTargetCacheInfo> tcpForwardTargetCaching, SignInStateInfo signInStateInfo, IClientsTransfer clientsTransfer)
        {
            this.clientInfoCaching = clientInfoCaching;
            this.tcpForwardTargetCaching = tcpForwardTargetCaching;
            this.signInStateInfo = signInStateInfo;
            signInStateInfo.OnChange += (state) =>
            {
                tcpForwardTargetCaching.ClearConnection();
            };
            clientInfoCaching.OnOffline += (client) =>
            {
                tcpForwardTargetCaching.ClearConnection(client.Name);
            };
            this.clientsTransfer = clientsTransfer;
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
            if (cacheInfo.Name == "/")
            {
                return signInStateInfo.Connection;
            }

            if (clientInfoCaching.GetByName(cacheInfo.Name, out ClientInfo client))
            {
                if (client.Connection == null || client.Connection.Connected == false)
                {
                    clientsTransfer.ConnectClient(client);
                }
                return client.Connection;
            }
            return null;
        }
    }
}