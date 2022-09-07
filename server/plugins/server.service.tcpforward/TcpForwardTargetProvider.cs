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
                tcpForwardTargetCaching.ClearConnection(client.Name);
            });
        }
        public void Get(string host, TcpForwardInfo info)
        {
            GetTarget(tcpForwardTargetCaching.Get(host), info);
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
                    cacheInfo.Connection = clientRegisterCaching.GetByName(cacheInfo.Name)?.OnLineConnection;
                }
                info.Connection = cacheInfo.Connection;
                info.TargetEndpoint = cacheInfo.Endpoint;
            }
        }
    }
}