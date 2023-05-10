using client.messengers.clients;
using common.libs;
using common.libs.extends;
using common.server;
using common.user;
using System;
using System.Collections.Generic;

namespace client.service.users
{
    [MessengerIdRange((ushort)UsersMessengerIds.Min, (ushort)UsersMessengerIds.Max)]
    public sealed class UsersMessenger : IMessenger
    {
        private readonly IUserInfoCaching userInfoCaching;
        private readonly IClientInfoCaching clientInfoCaching;

        public UsersMessenger(IUserInfoCaching userInfoCaching, IClientInfoCaching clientInfoCaching)
        {
            this.userInfoCaching = userInfoCaching;
            this.clientInfoCaching = clientInfoCaching;
        }


        [MessengerId((ushort)UsersMessengerIds.SignIn)]
        public void SignIn(IConnection connection)
        {
            try
            {
                Dictionary<string, string> dic = connection.ReceiveRequestWrap.Payload.GetUTF8String().DeJson<Dictionary<string, string>>();

                if (clientInfoCaching.Get(connection.FromConnection.ConnectId, out ClientInfo client))
                {
                    connection.Write(Helper.FalseArray);
                    return;
                }
                client.UpdateArgs(dic, "account");
                client.UpdateArgs(dic, "password");

                if (userInfoCaching.GetUser(connection.FromConnection, out UserInfo user) == false)
                {
                    connection.Write(Helper.FalseArray);
                    return;
                }

                client.UserAccess = user.Access;

            }
            catch (Exception)
            {
                connection.Write(Helper.FalseArray);
            }

        }

    }
}
