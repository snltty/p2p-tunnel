using client.messengers.clients;
using client.messengers.relay;
using common.server;
using common.server.model;
using System.Collections.Concurrent;

namespace client.realize.messengers.relay
{
    public class SourceConnectionSelector : ISourceConnectionSelector
    {
        private readonly IClientInfoCaching clientInfoCaching;

        public SourceConnectionSelector(IClientInfoCaching clientInfoCaching)
        {
            this.clientInfoCaching = clientInfoCaching;
        }

        public IConnection SelectSource(IConnection connection, ulong relayid)
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

        public IConnection SelectTarget(IConnection connection,ulong relayid)
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

    public class ConnecRouteCaching : IConnecRouteCaching
    {
        ConcurrentDictionary<ulong, ulong[]> connectsDic = new ConcurrentDictionary<ulong, ulong[]>();

        public ConcurrentDictionary<ulong, ulong[]> Connects => connectsDic;
        public void AddConnects(ConnectsInfo connects)
        {
            connectsDic.AddOrUpdate(connects.Id, connects.Connects, (a, b) => connects.Connects);
        }

        public void Remove(ulong id)
        {
            connectsDic.TryRemove(id, out _);
        }
        public void Clear()
        {
            connectsDic.Clear();
        }

    }
}
