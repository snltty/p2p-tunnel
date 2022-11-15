using common.server.model;
using System.Collections.Concurrent;

namespace client.messengers.relay
{
    public interface IConnecRouteCaching
    {
        public ConcurrentDictionary<ulong, ConnectInfo[]> Connects { get; }

        public bool Get(ulong fromId, ulong toId, out RouteInfo route);

        public void AddConnects(ConnectsInfo connects);
        public void AddRoutes(RoutesInfo routes);

        public void Remove(ulong id);
        public void Clear();
    }
}
