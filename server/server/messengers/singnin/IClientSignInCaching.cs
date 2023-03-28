using common.libs;
using common.server;
using System;
using System.Collections.Generic;

namespace server.messengers.singnin
{
    public interface IClientSignInCaching
    {
        public Action<SignInCacheInfo> OnChanged { get; set; }
        public Action<SignInCacheInfo> OnOffline { get; set; }
        public int Count { get; }

        public ulong Add(SignInCacheInfo model);

        public int UserCount(ulong uid);
        public bool Get(ulong id, out SignInCacheInfo client);
        public bool Get(string groupid, string name, out SignInCacheInfo client);
        public IEnumerable<SignInCacheInfo> Get(string groupid);
        public List<SignInCacheInfo> Get();
        public void Remove(ulong id);

        public bool Notify(IConnection connection);
    }
}
