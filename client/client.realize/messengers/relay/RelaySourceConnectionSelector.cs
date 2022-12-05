using client.messengers.clients;
using client.messengers.relay;
using common.server;

namespace client.realize.messengers.relay
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class RelaySourceConnectionSelector : IRelaySourceConnectionSelector
    {
        private readonly IClientInfoCaching clientInfoCaching;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientInfoCaching"></param>
        /// <param name="connecRouteCaching"></param>
        public RelaySourceConnectionSelector(IClientInfoCaching clientInfoCaching, IClientConnectsCaching connecRouteCaching)
        {
            this.clientInfoCaching = clientInfoCaching;
            clientInfoCaching.OnOffline.Sub((client) =>
            {
                connecRouteCaching.Remove(client.Id);
            });
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="relayid"></param>
        /// <returns></returns>
        public IConnection Select(IConnection connection, ulong relayid)
        {
            if (relayid > 0)
            {
                if (clientInfoCaching.Get(relayid, out ClientInfo client))
                {
                    return client.Connection;
                }
            }
            return connection;
        }
    }

    
}
