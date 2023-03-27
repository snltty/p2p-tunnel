using common.server;
using common.server.model;
using common.udpforward;
using server.messengers;
using server.messengers.singnin;

namespace server.service.udpforward
{
    public sealed class ServerUdpForwardValidator : DefaultUdpForwardValidator, IUdpForwardValidator, ISignInAccess
    {
        private readonly common.udpforward.Config config;
        private readonly IServiceAccessValidator serviceAccessProvider;

        public ServerUdpForwardValidator(common.udpforward.Config config, IServiceAccessValidator serviceAccessProvider) : base(config)
        {
            this.config = config;
            this.serviceAccessProvider = serviceAccessProvider;
        }

        public EnumServiceAccess Access()
        {
            return config.ConnectEnable ? EnumServiceAccess.UdpForward : EnumServiceAccess.None;
        }

        public new bool Validate(IConnection connection)
        {
            return config.ConnectEnable || serviceAccessProvider.Validate(connection, EnumServiceAccess.UdpForward);
        }
    }

}
