using common.libs;
using System.Linq;

namespace common.tcpforward
{
    /// <summary>
    /// 
    /// </summary>
    public interface ITcpForwardValidator
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public bool Validate(TcpForwardInfo arg);
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
    public  class DefaultTcpForwardValidator : ITcpForwardValidator
    {
        private readonly Config config;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        public DefaultTcpForwardValidator(Config config)
        {
            this.config = config;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
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
