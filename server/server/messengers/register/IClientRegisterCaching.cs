using common.libs;
using common.server;
using System.Collections.Generic;

namespace server.messengers.register
{
    public interface IClientRegisterCaching
    {
        public SimpleSubPushHandler<RegisterCacheInfo> OnChanged { get; }
        public SimpleSubPushHandler<RegisterCacheInfo> OnOffline { get; }
        public int Count { get; }

        public ulong Add(RegisterCacheInfo model);

        public bool Get(ulong id, out RegisterCacheInfo client);
        public bool Get(string groupid, string name, out RegisterCacheInfo client);
        public IEnumerable<RegisterCacheInfo> Get(string groupid);
        public List<RegisterCacheInfo> Get();
        public void Remove(ulong id);

        public bool Notify(IConnection connection);
    }
}
