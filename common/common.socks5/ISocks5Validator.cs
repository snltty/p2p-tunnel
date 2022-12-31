using common.server;
using System;

namespace common.socks5
{
    /// <summary>
    /// 
    /// </summary>
    public interface ISocks5Validator
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        public bool Validate(Socks5Info info);
    }

    /// <summary>
    /// 
    /// </summary>
    public class DefaultSocks5Validator : ISocks5Validator
    {
        private readonly Config config;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        public DefaultSocks5Validator(Config config)
        {
            this.config = config;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        public bool Validate(Socks5Info info)
        {
            return config.ConnectEnable;
        }
    }

}
