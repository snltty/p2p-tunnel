using common.libs;
using common.libs.extends;
using common.server;
using System.Linq;
using System.Net;

namespace common.tcpforward
{
    public interface ITcpForwardValidator
    {
        public bool Validate(TcpForwardInfo arg);
        public bool Validate(IConnection connection);
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

            IPEndPoint endpoint = NetworkHelper.EndpointFromArray(arg.TargetEndpoint);
            if (config.LanConnectEnable == false && arg.ForwardType == TcpForwardTypes.PROXY && endpoint.IsLan())
            {
                return false;
            }
            if (config.PortWhiteList.Length > 0 && config.PortWhiteList.Contains(endpoint.Port) == false)
            {
                return false;
            }
            if (config.PortBlackList.Length > 0 && config.PortBlackList.Contains(endpoint.Port))
            {
                return false;
            }
            return true;
        }
        public bool Validate(IConnection connection)
        {
            return true;
        }
    }

}
