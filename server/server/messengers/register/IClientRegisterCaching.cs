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
        public IEnumerable<RegisterCacheInfo> GetBySameGroup(string groupid);
        public IEnumerable<RegisterCacheInfo> GetBySameGroup(string groupid, string name);
        public List<RegisterCacheInfo> GetAll();
        public void Remove(ulong id);

        public bool Notify(IConnection connection);
    }
}
