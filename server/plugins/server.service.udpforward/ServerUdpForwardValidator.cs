using common.server.model;
using common.udpforward;
using server.messengers;

namespace server.service.udpforward
{
    public sealed class ServerUdpForwardValidator : DefaultUdpForwardValidator, IUdpForwardValidator
    {
        private readonly common.udpforward.Config config;
        private readonly IServiceAccessValidator serviceAccessProvider;

        public ServerUdpForwardValidator(common.udpforward.Config config, IServiceAccessValidator serviceAccessProvider) : base(config)
        {
            this.config = config;
            this.serviceAccessProvider = serviceAccessProvider;
        }
        public new bool Validate(string key)
        {
            return config.ConnectEnable || serviceAccessProvider.Validate(key, EnumServiceAccess.UdpForward);
        }
    }

}
