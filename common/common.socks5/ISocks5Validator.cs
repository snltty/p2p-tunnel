using common.server;
using System;

namespace common.socks5
{
    public interface ISocks5Validator
    {
        public bool Validate(IConnection connection, Socks5Info info);
        public bool Validate(IConnection connection, Socks5Info info, Config config);
    }

    public class DefaultSocks5Validator : ISocks5Validator
    {
        private readonly Config config;
        public DefaultSocks5Validator(Config config)
        {
            this.config = config;
        }
        public bool Validate(IConnection connection, Socks5Info info)
        {
            return config.ConnectEnable;
        }

        public bool Validate(IConnection connection, Socks5Info info, Config config)
        {
            return config.ConnectEnable;
        }
    }

}
