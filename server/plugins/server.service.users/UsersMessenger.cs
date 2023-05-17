using common.libs;
using common.libs.extends;
using common.server;
using common.user;
using server.messengers.singnin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace server.service.users
{
    /// <summary>
    /// 服务端权限配置
    /// </summary>
    [MessengerIdRange((ushort)UsersMessengerIds.Min, (ushort)UsersMessengerIds.Max)]
    public sealed class UsersMessenger : IMessenger
    {
        private readonly IServiceAccessValidator serviceAccessValidator;
        private readonly IUserStore userStore;
        private readonly common.user.Config config;
        private readonly IUserInfoCaching userInfoCaching;
        private readonly IClientSignInCaching clientSignInCaching;

        public UsersMessenger(IServiceAccessValidator serviceAccessValidator, IUserStore userStore, common.user.Config config, IUserInfoCaching userInfoCaching, IClientSignInCaching clientSignInCaching)
        {
            this.serviceAccessValidator = serviceAccessValidator;
            this.userStore = userStore;
            this.config = config;
            this.userInfoCaching = userInfoCaching;
            this.clientSignInCaching = clientSignInCaching;
        }

        [MessengerId((ushort)UsersMessengerIds.Page)]
        public void Page(IConnection connection)
        {
            if (serviceAccessValidator.Validate(connection.ConnectId, (uint)EnumServiceAccess.Setting) == false)
            {
                return;
            }

            UserInfoPageModel userInfoPage = new UserInfoPageModel();
            userInfoPage.DeBytes(connection.ReceiveRequestWrap.Payload);

            List<UserInfo> users = userStore.Get(userInfoPage.Page, userInfoPage.PageSize, userInfoPage.Sort, userInfoPage.Account).Select(c => new UserInfo
            {
                Access = c.Access,
                Account = c.Account,
                AddTime = c.AddTime,
                Connections = null,
                EndTime = c.EndTime,
                ID = c.ID,
                NetFlow = c.NetFlow,
                Password = string.Empty,
                SentBytes = c.SentBytes,
                SignCount = (uint)c.Connections.Count,
                SignLimit = c.SignLimit,
            }).ToList();

            connection.WriteUTF8(new UserInfoPageResultModel
            {
                Count = userStore.Count(),
                Page = userInfoPage.Page,
                PageSize = userInfoPage.PageSize,
                Data = users
            }.ToJson());
        }

        [MessengerId((ushort)UsersMessengerIds.Add)]
        public void Add(IConnection connection)
        {
            if (serviceAccessValidator.Validate(connection.ConnectId, (uint)EnumServiceAccess.Setting) == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }

            UserInfo user = connection.ReceiveRequestWrap.Payload.GetUTF8String().DeJson<UserInfo>();
            bool res = userStore.Add(user);
            if (userStore.Get(user.ID, out UserInfo _user))
            {
                foreach (ulong connectionid in _user.Connections.Keys)
                {
                    if (clientSignInCaching.Get(connectionid, out SignInCacheInfo client))
                    {
                        client.UserAccess = user.Access;
                    }
                }
            }

            connection.Write(res ? Helper.TrueArray : Helper.FalseArray);
        }

        [MessengerId((ushort)UsersMessengerIds.Password)]
        public void Password(IConnection connection)
        {
            if (serviceAccessValidator.Validate(connection.ConnectId, (uint)EnumServiceAccess.Setting) == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }

            UserPasswordInfo passwordInfo = new UserPasswordInfo();
            passwordInfo.DeBytes(connection.ReceiveRequestWrap.Payload);

            bool res = userStore.UpdatePassword(passwordInfo.ID, passwordInfo.Password);
            connection.Write(res ? Helper.TrueArray : Helper.FalseArray);

        }
        [MessengerId((ushort)UsersMessengerIds.PasswordSelf)]
        public void PasswordSelf(IConnection connection)
        {
            if (userInfoCaching.GetUser(connection, out UserInfo user))
            {
                bool res = userStore.UpdatePassword(user.ID, connection.ReceiveRequestWrap.Payload.GetUTF8String());
                connection.Write(res ? Helper.TrueArray : Helper.FalseArray);
                return;
            }

            connection.Write(Helper.FalseArray);
        }

        [MessengerId((ushort)UsersMessengerIds.Remove)]
        public void Remove(IConnection connection)
        {
            if (serviceAccessValidator.Validate(connection.ConnectId, (uint)EnumServiceAccess.Setting) == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }
            bool res = false;
            try
            {
                res = userStore.Remove(connection.ReceiveRequestWrap.Payload.ToUInt64());
            }
            catch (Exception)
            {
            }

            connection.Write(res ? Helper.TrueArray : Helper.FalseArray);
        }

        [MessengerId((ushort)UsersMessengerIds.Info)]
        public void Info(IConnection connection)
        {
            if (userInfoCaching.GetUser(connection, out UserInfo user))
            {
                connection.Write(new UserInfo
                {
                    Access = user.Access,
                    Account = user.Account,
                    AddTime = user.AddTime,
                    Connections = null,
                    EndTime = user.EndTime,
                    ID = user.ID,
                    NetFlow = user.NetFlow,
                    Password = string.Empty,
                    SentBytes = user.SentBytes,
                    SignCount = (uint)user.Connections.Count,
                    SignLimit = user.SignLimit,
                }.ToJson().ToUTF8Bytes());
                return;
            }
            connection.Write(new UserInfo().ToJson().ToUTF8Bytes());
        }


        [MessengerId((ushort)UsersMessengerIds.SignIn)]
        public void SignIn(IConnection connection)
        {

            UserSignInfo userSignInfo = new UserSignInfo();
            userSignInfo.DeBytes(connection.ReceiveRequestWrap.Payload);
            if (userStore.Get(userSignInfo.UserId, out UserInfo user))
            {
                if (user.Connections.TryGetValue(userSignInfo.ConnectionId, out IConnection _connection) && _connection != null && _connection.Connected)
                {
                    connection.Write(Helper.TrueArray);
                    return;
                }
            }
            connection.Write(Helper.FalseArray);
        }


        [MessengerId((ushort)UsersMessengerIds.GetSetting)]
        public async Task GetSetting(IConnection connection)
        {
            connection.WriteUTF8(await config.ReadString());
        }

        [MessengerId((ushort)UsersMessengerIds.Setting)]
        public async Task Setting(IConnection connection)
        {
            if (serviceAccessValidator.Validate(connection.ConnectId, (uint)EnumServiceAccess.Setting) == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }
            string jsonStr = connection.ReceiveRequestWrap.Payload.GetUTF8String();
            await config.SaveConfig(jsonStr);

            connection.Write(Helper.TrueArray);
        }
    }
}
