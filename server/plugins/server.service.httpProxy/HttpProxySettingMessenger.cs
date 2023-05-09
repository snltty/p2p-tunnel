using common.libs.extends;
using common.libs;
using common.server;
using server.messengers.singnin;
using server.messengers;
using System.Threading.Tasks;
using common.httpProxy;

namespace server.service.httpProxy
{
    [MessengerIdRange((ushort)HttpProxyMessengerIds.Min, (ushort)HttpProxyMessengerIds.Max)]
    public sealed class HttpProxySettingMessenger : IMessenger
    {
        private readonly common.httpProxy.Config config;
        private readonly IClientSignInCaching clientSignInCaching;
        private readonly IServiceAccessValidator serviceAccessValidator;
        public HttpProxySettingMessenger(common.httpProxy.Config config, IClientSignInCaching clientSignInCaching, IServiceAccessValidator serviceAccessValidator)
        {
            this.config = config;
            this.clientSignInCaching = clientSignInCaching;
            this.serviceAccessValidator = serviceAccessValidator;
        }

        [MessengerId((ushort)HttpProxyMessengerIds.GetSetting)]
        public async Task GetSetting(IConnection connection)
        {
            connection.WriteUTF8(await config.ReadString());
        }

        [MessengerId((ushort)HttpProxyMessengerIds.Setting)]
        public async Task Setting(IConnection connection)
        {
            string str = connection.ReceiveRequestWrap.Payload.GetUTF8String();

            if (clientSignInCaching.Get(connection.ConnectId, out SignInCacheInfo client) == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }
            if (serviceAccessValidator.Validate(connection, (uint)common.server.EnumServiceAccess.Setting) == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }

            await config.SaveConfig(str);

            connection.Write(Helper.TrueArray);
        }
    }
}
