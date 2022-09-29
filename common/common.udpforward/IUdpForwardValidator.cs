using common.libs;
using common.libs.extends;
using common.server;
using System.Linq;
using System.Net;

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

            IPEndPoint endpoint = NetworkHelper.EndpointFromArray(info.TargetEndpoint);
            if (config.LanConnectEnable == false && endpoint.IsLan())
            {
                return false;
            }

            if (config.PortBlackList.Contains(endpoint.Port))
            {
                return false;
            }
            if (config.PortWhiteList.Length > 0 && !config.PortBlackList.Contains(endpoint.Port))
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
