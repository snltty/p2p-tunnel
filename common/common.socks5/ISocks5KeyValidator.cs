using common.server;

namespace common.socks5
{
    public interface ISocks5KeyValidator
    {
        public bool Validate(IConnection connection, Socks5Info info);
    }

    public class DefaultSocks5KeyValidator : ISocks5KeyValidator
    {

        public DefaultSocks5KeyValidator()
        {

        }
        public bool Validate(IConnection connection, Socks5Info info)
        {
            return true;
        }
    }

}
