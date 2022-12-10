using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using server.messengers;
using server.messengers.register;
using System.Threading.Tasks;

namespace server.service.messengers.register
{
    /// <summary>
    /// 服务端配置
    /// </summary>
    [MessengerIdRange((ushort)RegisterMessengerIds.Min, (ushort)RegisterMessengerIds.Max)]
    public sealed class SettingMessenger : IMessenger
    {
        private readonly IClientRegisterCaching clientRegisterCaching;
        private readonly IServiceAccessValidator serviceAccessValidator;
        private readonly Config config;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientRegisterCaching"></param>
        /// <param name="serviceAccessValidator"></param>
        /// <param name="config"></param>
        public SettingMessenger(IClientRegisterCaching clientRegisterCaching, IServiceAccessValidator serviceAccessValidator, Config config)
        {
            this.clientRegisterCaching = clientRegisterCaching;
            this.serviceAccessValidator = serviceAccessValidator;
            this.config = config;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)RegisterMessengerIds.GetSetting)]
        public async Task GetSetting(IConnection connection)
        {
            if (clientRegisterCaching.Get(connection.ConnectId, out RegisterCacheInfo client) == false)
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
        [MessengerId((ushort)RegisterMessengerIds.Setting)]
        public async Task<byte[]> Setting(IConnection connection)
        {
            if (clientRegisterCaching.Get(connection.ConnectId, out RegisterCacheInfo client) == false)
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
