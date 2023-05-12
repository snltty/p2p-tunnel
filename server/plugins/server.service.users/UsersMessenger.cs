using common.libs;
using common.libs.extends;
using common.server;
using common.user;
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
        private readonly Config config;
        private readonly IUserInfoCaching userInfoCaching;

        public UsersMessenger(IServiceAccessValidator serviceAccessValidator, IUserStore userStore, Config config, IUserInfoCaching userInfoCaching)
        {
            this.serviceAccessValidator = serviceAccessValidator;
            this.userStore = userStore;
            this.config = config;
            this.userInfoCaching = userInfoCaching;
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

            List<UserInfo> users = userStore.Get(userInfoPage.Page, userInfoPage.PageSize, userInfoPage.Sort, userInfoPage.Account).ToList().ToJson().DeJson<List<UserInfo>>();
            foreach (UserInfo item in users)
            {
                item.Password = string.Empty;
            }

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

            bool res = userStore.Add(connection.ReceiveRequestWrap.Payload.GetUTF8String().DeJson<UserInfo>());

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
                UserInfo _user = user.ToJson().DeJson<UserInfo>();
                _user.Password = string.Empty;
                connection.Write(_user.ToJson().ToUTF8Bytes());
                return;
            }
            connection.Write(new UserInfo().ToJson().ToUTF8Bytes());
        }


        [MessengerId((ushort)UsersMessengerIds.SignIn)]
        public void SignIn(IConnection connection)
        {

            UserSignInfo userSignInfo = new UserSignInfo();
            userSignInfo.DeBytes(connection.ReceiveRequestWrap.Payload);
            if (userStore.Get(userSignInfo.ID, out UserInfo user))
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
