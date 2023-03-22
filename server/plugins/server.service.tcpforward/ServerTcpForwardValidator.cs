using common.server;
using common.server.model;
using common.tcpforward;
using server.messengers;

namespace server.service.tcpforward
{
    public sealed class ServerTcpForwardValidator : DefaultTcpForwardValidator, ITcpForwardValidator
    {
        private readonly common.tcpforward.Config config;
        private readonly IServiceAccessValidator serviceAccessProvider;
        public ServerTcpForwardValidator(common.tcpforward.Config config, IServiceAccessValidator serviceAccessProvider) : base(config)
        {
            this.config = config;
            this.serviceAccessProvider = serviceAccessProvider;
        }

        public new bool Validate(IConnection connection)
        {
            return config.ConnectEnable || serviceAccessProvider.Validate(connection, EnumServiceAccess.TcpForward);
        }
    }

}
