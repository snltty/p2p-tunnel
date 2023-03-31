using System;
using System.Collections.Generic;

namespace server.messengers.singnin
{
    public interface IClientSignInCaching
    {
        public Action<SignInCacheInfo> OnChanged { get; set; }
        public Action<SignInCacheInfo> OnLine { get; set; }
        public Action<SignInCacheInfo> OnOffline { get; set; }
        public int Count { get; }

        public SignInCacheInfo Add(SignInCacheInfo model);

        public bool Get(ulong connectionid, out SignInCacheInfo client);
        public bool Get(string groupid, string name, out SignInCacheInfo client);
        public IEnumerable<SignInCacheInfo> Get(string groupid);
        public List<SignInCacheInfo> Get();
        public void Remove(ulong connectionid);

        public bool Notify(ulong connectionid);
    }
}
