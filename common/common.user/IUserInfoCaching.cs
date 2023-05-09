using common.server;

namespace common.user
{
    public interface IUserInfoCaching
    {
        public bool GetUser(IConnection connection, out UserInfo user);
    }
}
