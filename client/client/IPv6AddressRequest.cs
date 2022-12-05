using System.Net;

namespace client
{
    /// <summary>
    /// 获取ipv6
    /// </summary>
    public interface IIPv6AddressRequest
    {
        /// <summary>
        /// 获取ipv6
        /// </summary>
        /// <returns></returns>
        public IPAddress[] GetIPV6();
    }
}
