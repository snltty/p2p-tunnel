using client.messengers.relay;
using common.server.model;
using System.Collections.Concurrent;

namespace client.realize.messengers.relay
{
    public sealed class ClientConnectsCaching : IClientConnectsCaching
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
