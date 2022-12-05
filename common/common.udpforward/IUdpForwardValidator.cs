using common.libs;
using System.Linq;

namespace common.udpforward
{
    /// <summary>
    /// 
    /// </summary>
    public interface IUdpForwardValidator
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public bool Validate(UdpForwardInfo info);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Validate(string key);
    }
    /// <summary>
    /// 
    /// </summary>
    public class DefaultUdpForwardValidator : IUdpForwardValidator
    {
        private readonly Config config;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        public DefaultUdpForwardValidator(Config config)
        {
            this.config = config;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Validate(string key)
        {
            return true;
        }
    }

}
