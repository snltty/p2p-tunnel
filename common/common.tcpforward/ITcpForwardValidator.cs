using common.libs;
using System.Linq;

namespace common.tcpforward
{
    public interface ITcpForwardValidator
    {
        public bool Validate(TcpForwardInfo arg);
        public bool Validate(string key);
    }

    public class DefaultTcpForwardValidator : ITcpForwardValidator
    {
        private readonly Config config;
        public DefaultTcpForwardValidator(Config config)
        {
            this.config = config;
        }
        public bool Validate(TcpForwardInfo arg)
        {
            if (config.ConnectEnable == false)
            {
                return false;
            }

            ushort port = NetworkHelper.PortFromArray(arg.TargetEndpoint);
            if (config.PortWhiteList.Length > 0 && config.PortWhiteList.Contains(port) == false)
            {
                return false;
            }
            if (config.PortBlackList.Length > 0 && config.PortBlackList.Contains(port))
            {
                return false;
            }
            return true;
        }
        public bool Validate(string key)
        {
            return true;
        }
    }

}
