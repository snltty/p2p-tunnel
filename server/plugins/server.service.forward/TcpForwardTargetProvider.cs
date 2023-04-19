using common.tcpforward;
using server.messengers.singnin;

namespace server.service.tcpforward
{
    internal sealed class TcpForwardTargetProvider : ITcpForwardTargetProvider
    {
        private readonly IClientSignInCaching clientSignInCaching;
        private readonly ITcpForwardTargetCaching<TcpForwardTargetCacheInfo> tcpForwardTargetCaching;

        public TcpForwardTargetProvider(IClientSignInCaching clientSignInCaching, ITcpForwardTargetCaching<TcpForwardTargetCacheInfo> tcpForwardTargetCaching)
        {
            this.clientSignInCaching = clientSignInCaching;
            this.tcpForwardTargetCaching = tcpForwardTargetCaching;

            clientSignInCaching.OnOffline+=(client) =>
            {
                tcpForwardTargetCaching.ClearConnection(client.ConnectionId);
            };
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
                    if(clientSignInCaching.Get(cacheInfo.Id,out SignInCacheInfo client))
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