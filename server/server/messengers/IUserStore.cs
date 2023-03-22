using System.Collections.Generic;

namespace server.messengers
{
    public interface IUserStore
    {
        public UserInfo DefaultUser { get; }
        public IEnumerable<UserInfo> Get();
        public IEnumerable<UserInfo> Get(int p = 1, int ps = 10);
        public bool Get(ulong uid, out UserInfo user);
        public bool Get(string account, string password, out UserInfo user);

        public bool Add(UserInfo user);
        public bool Remove(ulong uid);
    }
}
