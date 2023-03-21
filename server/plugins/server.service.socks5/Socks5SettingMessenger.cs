using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using common.socks5;
using server.messengers;
using server.messengers.singnin;
using System.Threading.Tasks;

namespace server.service.socks5
{
    /// <summary>
    /// socks5服务端配置
    /// </summary>
    [MessengerIdRange((ushort)Socks5MessengerIds.Min, (ushort)Socks5MessengerIds.Max)]
    public sealed class Socks5SettingMessenger : IMessenger
    {
        private readonly common.socks5.Config config;
        private readonly IClientSignInCaching clientSignInCaching;
        private readonly IServiceAccessValidator serviceAccessValidator;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        /// <param name="clientSignInCaching"></param>
        /// <param name="serviceAccessValidator"></param>
        public Socks5SettingMessenger(common.socks5.Config config, IClientSignInCaching clientSignInCaching, IServiceAccessValidator serviceAccessValidator)
        {
            this.config = config;
            this.clientSignInCaching = clientSignInCaching;
            this.serviceAccessValidator = serviceAccessValidator;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)Socks5MessengerIds.GetSetting)]
        public async Task GetSetting(IConnection connection)
        {
            connection.WriteUTF8(await config.ReadString());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)Socks5MessengerIds.Setting)]
        public async Task<byte[]> Setting(IConnection connection)
        {
            string str = connection.ReceiveRequestWrap.Payload.GetUTF8String();

            if (clientSignInCaching.Get(connection.ConnectId, out SignInCacheInfo client) == false)
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
