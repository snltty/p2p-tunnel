using client.messengers.clients;
using common.server;
using common.user;
using System.Collections.Generic;

namespace client.service.users
{
    public sealed class UserInfoCaching : IUserInfoCaching
    {
        private readonly IClientInfoCaching clientInfoCaching;
        private IUserStore userStore;

        public UserInfoCaching(IClientInfoCaching clientInfoCaching, IUserStore userStore)
        {
            this.clientInfoCaching = clientInfoCaching;
            this.userStore = userStore;
        }

        public bool GetUser(IConnection connection, out UserInfo user)
        {
            user = default;
            if (clientInfoCaching.Get(connection.ConnectId, out ClientInfo client))
            {
                return GetUser(client.Args, out user);
            }
            return false;
        }
        private bool GetUser(Dictionary<string, string> args, out UserInfo user)
        {
            user = default;
            if (GetAccountPassword(args, out string account, out string password))
            {
                return userStore.Get(account, password, out user);
            }
            return false;
        }
        private bool GetAccountPassword(Dictionary<string, string> args, out string account, out string password)
        {
            bool res = args.TryGetValue("account", out account);
            bool res1 = args.TryGetValue("password", out password);
            return res && res1;
        }
    }
}
