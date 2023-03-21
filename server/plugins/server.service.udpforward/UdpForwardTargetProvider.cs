using common.udpforward;
using server.messengers.singnin;

namespace server.service.udpforward
{
    /// <summary>
    /// 
    /// </summary>
    internal sealed class UdpForwardTargetProvider : IUdpForwardTargetProvider
    {
        private readonly IClientSignInCaching clientSignInCaching;
        private readonly IUdpForwardTargetCaching<UdpForwardTargetCacheInfo> udpForwardTargetCaching;

        public UdpForwardTargetProvider(IClientSignInCaching clientSignInCaching, IUdpForwardTargetCaching<UdpForwardTargetCacheInfo> udpForwardTargetCaching)
        {
            this.clientSignInCaching = clientSignInCaching;
            this.udpForwardTargetCaching = udpForwardTargetCaching;

            clientSignInCaching.OnOffline.Sub((client) =>
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
                    if (clientSignInCaching.Get(cacheInfo.Id, out SignInCacheInfo client))
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