using System;
using System.Linq;
using System.Net;

namespace common.libs.extends
{
    /// <summary>
    /// 
    /// </summary>
    public static class EndPointExtends
    {
        static Memory<byte> ipv6Loopback = IPAddress.IPv6Loopback.GetAddressBytes();
        static Memory<byte> ipv6Multicast = IPAddress.Parse("ff00::").GetAddressBytes();
        static Memory<byte> ipv6Local = IPAddress.Parse("fe80::").GetAddressBytes();
        /// <summary>
        /// 判断是不是本地地址
        /// </summary>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        public static bool IsLan(this IPEndPoint endPoint)
        {
            if (endPoint == null) return false;
            return endPoint.Address.IsLan();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="adress"></param>
        /// <returns></returns>
        public static bool IsLan(this IPAddress address)
        {
            if (address == null) return false;

            var bytes = address.GetAddressBytes().AsSpan();
            if (bytes.Length == 4)
            {
                return bytes[0] == 127
               || bytes[0] == 10
               || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
               || (bytes[0] == 192 && bytes[1] == 168);
            }

            return bytes.SequenceEqual(ipv6Loopback.Span)
                || bytes.SequenceEqual(ipv6Multicast.Span)
                || (bytes[0] == ipv6Local.Span[0] && bytes[1] == ipv6Local.Span[1]);
        }

        public static int Length(this IPAddress address)
        {
            if (address == null) return 0;
            else if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) return 4;
            return 16;
        }
    }
}
