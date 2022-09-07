using common.udpforward;
using server.messengers.register;

namespace server.service.udpforward
{
    internal class UdpForwardTargetProvider : IUdpForwardTargetProvider
    {
        private readonly IClientRegisterCaching clientRegisterCaching;
        private readonly IUdpForwardTargetCaching<UdpForwardTargetCacheInfo> udpForwardTargetCaching;

        public UdpForwardTargetProvider(IClientRegisterCaching clientRegisterCaching, IUdpForwardTargetCaching<UdpForwardTargetCacheInfo> udpForwardTargetCaching)
        {
            this.clientRegisterCaching = clientRegisterCaching;
            this.udpForwardTargetCaching = udpForwardTargetCaching;

            clientRegisterCaching.OnOffline.Sub((client) =>
            {
                udpForwardTargetCaching.ClearConnection(client.Name);
            });
        }

        public void Get(int sourcePort, UdpForwardInfo info)
        {
            UdpForwardTargetCacheInfo cacheInfo = udpForwardTargetCaching.Get(sourcePort);

            if (cacheInfo != null)
            {
                if (cacheInfo.Connection == null || !cacheInfo.Connection.Connected)
                {
                    cacheInfo.Connection = clientRegisterCaching.GetByName(cacheInfo.Name)?.OnLineConnection;
                }
                info.Connection = cacheInfo.Connection;
                info.TargetEndpoint = cacheInfo.Endpoint;
            }
        }
    }
}