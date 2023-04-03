using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using server.messengers;
using server.messengers.singnin;
using server.service.users.model;
using System;
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
        private readonly IClientSignInCaching clientSignInCaching;
        private readonly IServiceAccessValidator serviceAccessValidator;
        private readonly IUserStore userStore;
        private readonly Config config;
        public UsersMessenger(IClientSignInCaching clientSignInCaching, IServiceAccessValidator serviceAccessValidator, IUserStore userStore, Config config)
        {
            this.clientSignInCaching = clientSignInCaching;
            this.serviceAccessValidator = serviceAccessValidator;
            this.userStore = userStore;
            this.config = config;
        }

        [MessengerId((ushort)UsersMessengerIds.Page)]
        public void Page(IConnection connection)
        {
            if (clientSignInCaching.Get(connection.ConnectId, out SignInCacheInfo client) == false)
            {
                return;
            }
            if (serviceAccessValidator.Validate(connection, (uint)EnumServiceAccess.Setting) == false)
            {
                return;
            }

            UserInfoPageModel userInfoPage = new UserInfoPageModel();
            userInfoPage.DeBytes(connection.ReceiveRequestWrap.Payload);

            connection.WriteUTF8(new UserInfoPageResultModel
            {
                Count = userStore.Count(),
                Page = userInfoPage.Page,
                PageSize = userInfoPage.PageSize,
                Data = userStore.Get(userInfoPage.Page, userInfoPage.PageSize, userInfoPage.Sort, userInfoPage.Account).ToList()
            }.ToJson());
        }

        [MessengerId((ushort)UsersMessengerIds.Add)]
        public void Add(IConnection connection)
        {
            if (clientSignInCaching.Get(connection.ConnectId, out SignInCacheInfo client) == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }
            if (serviceAccessValidator.Validate(connection, (uint)EnumServiceAccess.Setting) == false)
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
            if (clientSignInCaching.Get(connection.ConnectId, out SignInCacheInfo client) == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }
            if (SignInAccessValidator.GetAccountPassword(client.Args, out string account, out string password) && userStore.Get(account, password, out UserInfo user))
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
            if (clientSignInCaching.Get(connection.ConnectId, out SignInCacheInfo client) == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }
            if (serviceAccessValidator.Validate(connection, (uint)EnumServiceAccess.Setting) == false)
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


        [MessengerId((ushort)UsersMessengerIds.GetSetting)]
        public async Task GetSetting(IConnection connection)
        {
            connection.WriteUTF8(await config.ReadString());
        }

        [MessengerId((ushort)UsersMessengerIds.Setting)]
        public async Task Setting(IConnection connection)
        {
            if (clientSignInCaching.Get(connection.ConnectId, out SignInCacheInfo client) == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }
            if (serviceAccessValidator.Validate(connection, (uint)EnumServiceAccess.Setting) == false)
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
