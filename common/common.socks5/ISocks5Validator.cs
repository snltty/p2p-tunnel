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
        public bool Validate(IConnection connection, Socks5Info info);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="info"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public bool Validate(IConnection connection, Socks5Info info, Config config);
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
        public bool Validate(IConnection connection, Socks5Info info)
        {
            return config.ConnectEnable;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="info"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public bool Validate(IConnection connection, Socks5Info info, Config config)
        {
            return config.ConnectEnable;
        }
    }

}
