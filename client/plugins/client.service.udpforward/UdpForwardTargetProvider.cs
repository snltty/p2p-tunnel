using client.messengers.clients;
using client.messengers.register;
using common.server;
using common.udpforward;

namespace client.service.udpforward
{
    /// <summary>
    /// udp转发目标提供
    /// </summary>
    internal sealed class UdpForwardTargetProvider : IUdpForwardTargetProvider
    {
        private readonly IClientInfoCaching clientInfoCaching;
        private readonly IUdpForwardTargetCaching<UdpForwardTargetCacheInfo> udpForwardTargetCaching;
        private readonly RegisterStateInfo registerStateInfo;

        public UdpForwardTargetProvider(IClientInfoCaching clientInfoCaching, IUdpForwardTargetCaching<UdpForwardTargetCacheInfo> udpForwardTargetCaching, RegisterStateInfo registerStateInfo)
        {
            this.clientInfoCaching = clientInfoCaching;
            this.udpForwardTargetCaching = udpForwardTargetCaching;
            this.registerStateInfo = registerStateInfo;
            registerStateInfo.OnRegisterStateChange.Sub((state) =>
            {
                udpForwardTargetCaching.ClearConnection();
            });
            clientInfoCaching.OnOffline.Sub((client) =>
            {
                udpForwardTargetCaching.ClearConnection(client.Name);
            });
        }

        /// <summary>
        /// 根据端口获取目标连接
        /// </summary>
        /// <param name="port"></param>
        /// <param name="info"></param>
        public void Get(ushort port, UdpForwardInfo info)
        {
            UdpForwardTargetCacheInfo cacheInfo = udpForwardTargetCaching.Get(port);
            if (cacheInfo == null)
            {
                return;
            }
            if (cacheInfo.Connection == null || !cacheInfo.Connection.Connected)
            {
                cacheInfo.Connection = SelectConnection(cacheInfo);
            }
            info.Connection = cacheInfo.Connection;
            info.TargetEndpoint = cacheInfo.Endpoint;
        }

        private IConnection SelectConnection(UdpForwardTargetCacheInfo cacheInfo)
        {
            if (string.IsNullOrWhiteSpace(cacheInfo.Name))
            {
                return registerStateInfo.Connection;
            }

            if (clientInfoCaching.GetByName(cacheInfo.Name, out ClientInfo client))
            {
                return client.Connection;
            }
            return null;
        }
    }
}