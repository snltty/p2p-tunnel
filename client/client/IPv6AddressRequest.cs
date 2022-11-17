using System.Net;

namespace client
{
    /// <summary>
    /// 获取ipv6
    /// </summary>
    public interface IIPv6AddressRequest
    {
        public IPAddress[] GetIPV6();
    }
}
