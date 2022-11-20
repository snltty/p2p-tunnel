using common.tcpforward;
using server.messengers.register;

namespace server.service.tcpforward
{
    internal class TcpForwardTargetProvider : ITcpForwardTargetProvider
    {
        private readonly IClientRegisterCaching clientRegisterCaching;
        private readonly ITcpForwardTargetCaching<TcpForwardTargetCacheInfo> tcpForwardTargetCaching;

        public TcpForwardTargetProvider(IClientRegisterCaching clientRegisterCaching, ITcpForwardTargetCaching<TcpForwardTargetCacheInfo> tcpForwardTargetCaching)
        {
            this.clientRegisterCaching = clientRegisterCaching;
            this.tcpForwardTargetCaching = tcpForwardTargetCaching;

            clientRegisterCaching.OnOffline.Sub((client) =>
            {
                tcpForwardTargetCaching.ClearConnection(client.Id);
            });
        }
        public void Get(string host, TcpForwardInfo info)
        {
            GetTarget(tcpForwardTargetCaching.Get(host), info);
        }
        public void Get(ushort port, TcpForwardInfo info)
        {
            GetTarget(tcpForwardTargetCaching.Get(port), info);
        }

        private void GetTarget(TcpForwardTargetCacheInfo cacheInfo, TcpForwardInfo info)
        {
            if (cacheInfo != null)
            {
                if (cacheInfo.Connection == null || cacheInfo.Connection.Connected == false)
                {
                    if(clientRegisterCaching.Get(cacheInfo.Id,out RegisterCacheInfo client))
                    {
                        cacheInfo.Connection = client.OnLineConnection;
                    }
                }
                info.Connection = cacheInfo.Connection;
                info.TargetEndpoint = cacheInfo.Endpoint;
            }
        }
    }
}