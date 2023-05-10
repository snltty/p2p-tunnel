using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
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
        private readonly ITcpServer tcpServer;

        public SettingMessenger(IClientSignInCaching clientSignInCaching, IServiceAccessValidator serviceAccessValidator, Config config, ITcpServer tcpServer)
        {
            this.clientSignInCaching = clientSignInCaching;
            this.serviceAccessValidator = serviceAccessValidator;
            this.config = config;
            this.tcpServer = tcpServer;
        }

        [MessengerId((ushort)SignInMessengerIds.GetSetting)]
        public async Task GetSetting(IConnection connection)
        {
            if (clientSignInCaching.Get(connection.ConnectId, out SignInCacheInfo client) == false)
            {
                return;
            }
            if (serviceAccessValidator.Validate(connection.ConnectId, (uint)common.server.EnumServiceAccess.Setting) == false)
            {
                return;
            }
            string str = await config.ReadString();
            connection.WriteUTF8(str);
        }

        [MessengerId((ushort)SignInMessengerIds.Setting)]
        public async Task Setting(IConnection connection)
        {
            if (clientSignInCaching.Get(connection.ConnectId, out SignInCacheInfo client) == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }
            if (serviceAccessValidator.Validate(connection.ConnectId, (uint)common.server.EnumServiceAccess.Setting) == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }

            string str = connection.ReceiveRequestWrap.Payload.GetUTF8String();
            await config.SaveConfig(str);

            tcpServer.SetBufferSize((1 << (byte)config.TcpBufferSize) * 1024);

            connection.Write(Helper.TrueArray);
        }
    }
}
