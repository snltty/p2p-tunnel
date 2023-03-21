using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using server.messengers;
using server.messengers.singnin;
using System.Threading.Tasks;

namespace server.service.messengers.singnin
{
    /// <summary>
    /// 服务端配置
    /// </summary>
    [MessengerIdRange((ushort)SignInMessengerIds.Min, (ushort)SignInMessengerIds.Max)]
    public sealed class SettingMessenger : IMessenger
    {
        private readonly IClientSignInCaching clientSignInCaching;
        private readonly IServiceAccessValidator serviceAccessValidator;
        private readonly Config config;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientSignInCaching"></param>
        /// <param name="serviceAccessValidator"></param>
        /// <param name="config"></param>
        public SettingMessenger(IClientSignInCaching clientSignInCaching, IServiceAccessValidator serviceAccessValidator, Config config)
        {
            this.clientSignInCaching = clientSignInCaching;
            this.serviceAccessValidator = serviceAccessValidator;
            this.config = config;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)SignInMessengerIds.GetSetting)]
        public async Task GetSetting(IConnection connection)
        {
            if (clientSignInCaching.Get(connection.ConnectId, out SignInCacheInfo client) == false)
            {
                return;
            }
            if (serviceAccessValidator.Validate(client.GroupId, EnumServiceAccess.Setting) == false)
            {
                return;
            }
            string str = await config.ReadString();
            connection.WriteUTF8(str);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)SignInMessengerIds.Setting)]
        public async Task<byte[]> Setting(IConnection connection)
        {
            if (clientSignInCaching.Get(connection.ConnectId, out SignInCacheInfo client) == false)
            {
                return Helper.FalseArray;
            }
            if (serviceAccessValidator.Validate(client.GroupId, EnumServiceAccess.Setting) == false)
            {
                return Helper.FalseArray;
            }

            string str = connection.ReceiveRequestWrap.Payload.GetUTF8String();
            await config.SaveConfig(str);

            return Helper.TrueArray;
        }
    }
}
