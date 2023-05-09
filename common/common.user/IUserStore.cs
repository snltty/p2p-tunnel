using System.Collections.Generic;

namespace common.user
{
    public interface IUserStore
    {
        public int Count();
        public IEnumerable<UserInfo> Get();
        public IEnumerable<UserInfo> Get(int p = 1, int ps = 10, byte sort = 0, string account = "");
        public bool Get(ulong uid, out UserInfo user);
        public bool Get(string account, string password, out UserInfo user);

        public bool Add(UserInfo user);
        public bool UpdatePassword(ulong id, string password);
        public bool Remove(ulong uid);

        public UserInfo DefaultUser();
    }
}
