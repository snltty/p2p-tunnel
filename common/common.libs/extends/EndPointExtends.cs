using System;
using System.Buffers.Binary;
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
        /// 判断是不是本地地址
        /// </summary>
        /// <param name="adress"></param>
        /// <returns></returns>
        public static bool IsLan(this IPAddress address)
        {
            if (address == null) return false;

            return IsLan(address.GetAddressBytes().AsSpan());
        }

        public static bool IsLan(this Memory<byte> address)
        {
            return IsLan(address.Span);
        }

        public static bool IsLan(Span<byte> address)
        {
            if (address.Length < 4) return false;
            if (address.Length == 4)
            {
                return address[0] == 127
               || address[0] == 10
               || (address[0] == 172 && address[1] >= 16 && address[1] <= 31)
               || (address[0] == 192 && address[1] == 168);
            }
            return address.Length == ipv6Loopback.Length && (address.SequenceEqual(ipv6Loopback.Span)
                || address.SequenceEqual(ipv6Multicast.Span)
                || (address[0] == ipv6Local.Span[0] && address[1] == ipv6Local.Span[1]));
        }


        public static bool GetIsBroadcastAddress(this IPAddress address)
        {
            return address.GetAddressBytes().AsSpan().GetIsBroadcastAddress();
        }
        public static bool GetIsBroadcastAddress(this Memory<byte> address)
        {
            return address.Span.GetIsBroadcastAddress();
        }
        public static bool GetIsBroadcastAddress(this Span<byte> address)
        {
            uint ip = BinaryPrimitives.ReadUInt32BigEndian(address);
            return ip >= 0xE0000000 && ip <= 0xEFFFFFFF;
        }


        public static bool GetIsAnyAddress(this IPAddress address)
        {
            return address.GetAddressBytes().AsSpan().GetIsAnyAddress();
        }
        public static bool GetIsAnyAddress(this Memory<byte> address)
        {
            return address.Span.GetIsAnyAddress();
        }
        public static bool GetIsAnyAddress(this Span<byte> address)
        {
            return (address.Length == 4 && address.SequenceEqual(Helper.AnyIpArray))
                || (address.Length == 6 && address.SequenceEqual(Helper.AnyIpv6Array));
        }

        public static int Length(this IPAddress address)
        {
            if (address == null) return 0;
            else if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) return 4;
            return 16;
        }
    }
}
