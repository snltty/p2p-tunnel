using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using server.messengers;
using server.messengers.register;
using System.Threading.Tasks;

namespace server.service.socks5
{
    /// <summary>
    /// 服务端权限配置
    /// </summary>
    [MessengerIdRange((ushort)ServiceAccessValidatorMessengerIds.Min, (ushort)ServiceAccessValidatorMessengerIds.Max)]
    public sealed class ServiceAccessMessenger : IMessenger
    {
        private readonly IClientRegisterCaching clientRegisterCaching;
        private readonly IServiceAccessValidator serviceAccessValidator;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientRegisterCaching"></param>
        /// <param name="serviceAccessValidator"></param>
        public ServiceAccessMessenger(IClientRegisterCaching clientRegisterCaching, IServiceAccessValidator serviceAccessValidator)
        {
            this.clientRegisterCaching = clientRegisterCaching;
            this.serviceAccessValidator = serviceAccessValidator;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)ServiceAccessValidatorMessengerIds.GetSetting)]
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
            return (await serviceAccessValidator.ReadString()).ToBytes();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)ServiceAccessValidatorMessengerIds.Setting)]
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
            await serviceAccessValidator.SaveConfig(str);

            return Helper.TrueArray;
        }
    }
}
