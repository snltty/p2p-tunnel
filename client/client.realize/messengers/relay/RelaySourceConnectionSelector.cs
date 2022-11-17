using client.messengers.clients;
using client.messengers.relay;
using common.server;

namespace client.realize.messengers.relay
{
    public class RelaySourceConnectionSelector : IRelaySourceConnectionSelector
    {
        private readonly IClientInfoCaching clientInfoCaching;

        public RelaySourceConnectionSelector(IClientInfoCaching clientInfoCaching, IClientConnectsCaching connecRouteCaching)
        {
            this.clientInfoCaching = clientInfoCaching;
            clientInfoCaching.OnOffline.Sub((client) =>
            {
                connecRouteCaching.Remove(client.Id);
            });
        }

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
