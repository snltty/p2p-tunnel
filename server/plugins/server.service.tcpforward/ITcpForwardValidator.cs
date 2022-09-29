using common.server;
using common.tcpforward;

namespace server.service.tcpforward
{
    public class ServerTcpForwardValidator : DefaultTcpForwardValidator, ITcpForwardValidator
    {
        private readonly common.tcpforward.Config config;
        public ServerTcpForwardValidator(common.tcpforward.Config config) : base(config)
        {
            this.config = config;
        }
        public new bool Validate(IConnection connection)
        {
            return config.ConnectEnable;
        }
    }

}
