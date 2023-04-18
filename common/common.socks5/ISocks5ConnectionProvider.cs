using common.proxy;

namespace common.socks5
{
    public interface ISocks5ConnectionProvider
    {
        public void Get(ProxyInfo info);
    }
}
