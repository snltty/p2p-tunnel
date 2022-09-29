using common.server;
using common.udpforward;

namespace server.service.udpforward
{
    public class ServerUdpForwardValidator : DefaultUdpForwardValidator, IUdpForwardValidator
    {
        private readonly common.udpforward.Config config;
        public ServerUdpForwardValidator(common.udpforward.Config config) : base(config)
        {
            this.config = config;
        }
        public new bool Validate(IConnection connection)
        {
            return config.ConnectEnable;
        }
    }

}
