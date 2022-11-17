using common.server.model;
using System.Collections.Concurrent;

namespace client.messengers.relay
{
    public interface IConnecRouteCaching
    {
        public ConcurrentDictionary<ulong, ulong[]> Connects { get; }

        public void AddConnects(ConnectsInfo connects);

        public void Remove(ulong id);
        public void Clear();
    }
}
