using common.udpforward;
using server.messengers.register;

namespace server.service.udpforward
{
    /// <summary>
    /// 
    /// </summary>
    internal sealed class UdpForwardTargetProvider : IUdpForwardTargetProvider
    {
        private readonly IClientRegisterCaching clientRegisterCaching;
        private readonly IUdpForwardTargetCaching<UdpForwardTargetCacheInfo> udpForwardTargetCaching;

        public UdpForwardTargetProvider(IClientRegisterCaching clientRegisterCaching, IUdpForwardTargetCaching<UdpForwardTargetCacheInfo> udpForwardTargetCaching)
        {
            this.clientRegisterCaching = clientRegisterCaching;
            this.udpForwardTargetCaching = udpForwardTargetCaching;

            clientRegisterCaching.OnOffline.Sub((client) =>
            {
                udpForwardTargetCaching.ClearConnection(client.Id);
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourcePort"></param>
        /// <param name="info"></param>
        public void Get(ushort sourcePort, UdpForwardInfo info)
        {
            UdpForwardTargetCacheInfo cacheInfo = udpForwardTargetCaching.Get(sourcePort);

            if (cacheInfo != null)
            {
                if (cacheInfo.Connection == null || cacheInfo.Connection.Connected == false)
                {
                    if (clientRegisterCaching.Get(cacheInfo.Id, out RegisterCacheInfo client))
                    {
                        cacheInfo.Connection = client.Connection;
                    }
                }
                info.Connection = cacheInfo.Connection;
                info.TargetEndpoint = cacheInfo.Endpoint;
            }
        }
    }
}