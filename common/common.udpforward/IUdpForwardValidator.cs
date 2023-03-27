using common.libs;
using common.server;
using System.Linq;

namespace common.udpforward
{
    public interface IUdpForwardValidator
    {
        public bool Validate(UdpForwardInfo info);
        public bool Validate(IConnection connection);
    }
    public class DefaultUdpForwardValidator : IUdpForwardValidator
    {
        private readonly Config config;
        public DefaultUdpForwardValidator(Config config)
        {
            this.config = config;
        }

        public bool Validate(UdpForwardInfo info)
        {
            if (config.ConnectEnable == false)
            {
                return false;
            }

            int port = NetworkHelper.PortFromArray(info.TargetEndpoint);
            if (config.PortBlackList.Contains(port))
            {
                return false;
            }
            if (config.PortWhiteList.Length > 0 && config.PortBlackList.Contains(port) == false)
            {
                return false;
            }

            return true;
        }

        public bool Validate(IConnection connection)
        {
            return config.ConnectEnable;
        }
    }

}
