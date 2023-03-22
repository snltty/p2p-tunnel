using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using server.messengers;
using server.messengers.singnin;
using System.Threading.Tasks;

namespace server.service.socks5
{
    /// <summary>
    /// 服务端权限配置
    /// </summary>
    [MessengerIdRange((ushort)ServiceAccessValidatorMessengerIds.Min, (ushort)ServiceAccessValidatorMessengerIds.Max)]
    public sealed class ServiceAccessMessenger : IMessenger
    {
        private readonly IClientSignInCaching clientSignInCaching;
        private readonly IServiceAccessValidator serviceAccessValidator;
        public ServiceAccessMessenger(IClientSignInCaching clientSignInCaching, IServiceAccessValidator serviceAccessValidator)
        {
            this.clientSignInCaching = clientSignInCaching;
            this.serviceAccessValidator = serviceAccessValidator;
        }

        [MessengerId((ushort)ServiceAccessValidatorMessengerIds.GetSetting)]
        public async Task GetSetting(IConnection connection)
        {
            if (clientSignInCaching.Get(connection.ConnectId, out SignInCacheInfo client) == false)
            {
                return;
            }
            if (serviceAccessValidator.Validate(connection, EnumServiceAccess.Setting) == false)
            {
                return;
            }

            string str = await serviceAccessValidator.ReadString();
            connection.WriteUTF8(str);
        }

        [MessengerId((ushort)ServiceAccessValidatorMessengerIds.Setting)]
        public async Task<byte[]> Setting(IConnection connection)
        {
            if (clientSignInCaching.Get(connection.ConnectId, out SignInCacheInfo client) == false)
            {
                return Helper.FalseArray;
            }
            if (serviceAccessValidator.Validate(connection, EnumServiceAccess.Setting) == false)
            {
                return Helper.FalseArray;
            }

            string str = connection.ReceiveRequestWrap.Payload.GetUTF8String();
            await serviceAccessValidator.SaveConfig(str);

            return Helper.TrueArray;
        }
    }
}
