using common.libs;
using common.server;
using System.Collections.Generic;

namespace server.messengers.singnin
{
    public interface IClientSignInCaching
    {
        public SimpleSubPushHandler<SignInCacheInfo> OnChanged { get; }
        public SimpleSubPushHandler<SignInCacheInfo> OnOffline { get; }
        public int Count { get; }

        public ulong Add(SignInCacheInfo model);

        public bool Get(ulong id, out SignInCacheInfo client);
        public bool Get(string groupid, string name, out SignInCacheInfo client);
        public IEnumerable<SignInCacheInfo> Get(string groupid);
        public List<SignInCacheInfo> Get();
        public void Remove(ulong id);

        public bool Notify(IConnection connection);
    }
}
