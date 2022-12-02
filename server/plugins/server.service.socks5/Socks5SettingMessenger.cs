using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using common.socks5;
using server.messengers;
using server.messengers.register;
using System.Threading.Tasks;

namespace server.service.socks5
{
    [MessengerIdRange((ushort)Socks5MessengerIds.Min, (ushort)Socks5MessengerIds.Max)]
    public sealed class Socks5SettingMessenger : IMessenger
    {
        private readonly common.socks5.Config config;
        private readonly IClientRegisterCaching clientRegisterCaching;
        private readonly IServiceAccessValidator serviceAccessValidator;

        public Socks5SettingMessenger(common.socks5.Config config, IClientRegisterCaching clientRegisterCaching, IServiceAccessValidator serviceAccessValidator)
        {
            this.config = config;
            this.clientRegisterCaching = clientRegisterCaching;
            this.serviceAccessValidator = serviceAccessValidator;
        }

        [MessengerId((ushort)Socks5MessengerIds.GetSetting)]
        public async Task<byte[]> GetSetting(IConnection connection)
        {
            return (await config.ReadString()).ToBytes();
        }

        [MessengerId((ushort)Socks5MessengerIds.Setting)]
        public async Task<byte[]> Setting(IConnection connection)
        {
            string str = connection.ReceiveRequestWrap.Payload.GetString();

            if (clientRegisterCaching.Get(connection.ConnectId, out RegisterCacheInfo client) == false)
            {
                return Helper.FalseArray;
            }
            if (serviceAccessValidator.Validate(client.GroupId, EnumServiceAccess.Setting) == false)
            {
                return Helper.FalseArray;
            }

            await config.SaveConfig(str);

            return Helper.TrueArray;
        }
    }
}
