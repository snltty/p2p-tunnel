using System.Net;

namespace client
{
    public interface IIPv6AddressRequest
    {
        public IPAddress[] GetIPV6();
    }
}
