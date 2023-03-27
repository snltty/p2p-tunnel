using common.server;
using common.server.model;
using common.tcpforward;
using server.messengers;
using server.messengers.singnin;

namespace server.service.tcpforward
{

    public sealed class ServerTcpForwardValidator : DefaultTcpForwardValidator, ITcpForwardValidator, ISignInAccess
    {
        private readonly common.tcpforward.Config config;
        private readonly IServiceAccessValidator serviceAccessProvider;
        public ServerTcpForwardValidator(common.tcpforward.Config config, IServiceAccessValidator serviceAccessProvider) : base(config)
        {
            this.config = config;
            this.serviceAccessProvider = serviceAccessProvider;
        }

        public EnumServiceAccess Access()
        {
            return config.ConnectEnable ? EnumServiceAccess.TcpForward : EnumServiceAccess.None;
        }
        public new bool Validate(IConnection connection)
        {
            return config.ConnectEnable || serviceAccessProvider.Validate(connection, EnumServiceAccess.TcpForward);
        }
    }

}
