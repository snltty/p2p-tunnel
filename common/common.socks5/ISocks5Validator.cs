namespace common.socks5
{
    public interface ISocks5Validator
    {
        public bool Validate(Socks5Info info);
    }

    public class DefaultSocks5Validator : ISocks5Validator
    {
        private readonly Config config;
        public DefaultSocks5Validator(Config config)
        {
            this.config = config;
        }
        public bool Validate(Socks5Info info)
        {
            return config.ConnectEnable;
        }
    }

}
