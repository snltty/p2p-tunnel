using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using server.messengers;
using server.messengers.register;
using System.Threading.Tasks;

namespace server.service.messengers.register
{
    [MessengerIdRange((ushort)RegisterMessengerIds.Min, (ushort)RegisterMessengerIds.Max)]
    public sealed class SettingMessenger : IMessenger
    {
        private readonly IClientRegisterCaching clientRegisterCaching;
        private readonly IServiceAccessValidator serviceAccessValidator;
        private readonly Config config;

        public SettingMessenger(IClientRegisterCaching clientRegisterCaching, IServiceAccessValidator serviceAccessValidator, Config config)
        {
            this.clientRegisterCaching = clientRegisterCaching;
            this.serviceAccessValidator = serviceAccessValidator;
            this.config = config;
        }

        [MessengerId((ushort)RegisterMessengerIds.GetSetting)]
        public async Task<byte[]> GetSetting(IConnection connection)
        {
            if (clientRegisterCaching.Get(connection.ConnectId, out RegisterCacheInfo client) == false)
            {
                return Helper.EmptyArray;
            }
            if (serviceAccessValidator.Validate(client.GroupId, EnumServiceAccess.Setting) == false)
            {
                return Helper.EmptyArray;
            }
            return (await config.ReadString()).ToBytes();
        }

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

            string str = connection.ReceiveRequestWrap.Payload.GetString();
            await config.SaveConfig(str);

            return Helper.TrueArray;
        }
    }
}
