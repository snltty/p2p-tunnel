using common.libs;
using common.libs.extends;
using System;
using System.Buffers.Binary;
using System.Net;
using System.Text;

namespace common.socks5
{
    /// <summary>
    /// socks5 数据包解析和组装
    /// </summary>
    public class Socks5Parser
    {

        /// <summary>
        /// 获取客户端过来的支持的认证方式列表
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        public static Socks5EnumAuthType[] GetAuthMethods(ReadOnlySpan<byte> span)
        {
            //VER       NMETHODS    METHODS
            // 1            1       1-255
            //版本     支持哪些认证     一个认证方式一个字节
            Socks5EnumAuthType[] res = new Socks5EnumAuthType[span[1]];
            for (int i = 0; i < span.Length; i++)
            {
                res[i] = (Socks5EnumAuthType)span[i];
            }
            return res;
        }

        public static (string username, string password) GetPasswordAuthInfo(Span<byte> span)
        {
            /*
             子版本 username长度 username password长度 password
             0x01   
             */
            string username = span.Slice(2, span[1]).GetString();
            string password = span.Slice(2 + span[1] + 1, span[2 + span[1]]).GetString();
            return (username, password);
        }

        public static IPEndPoint GetRemoteEndPoint(Memory<byte> data)
        {
            if(data.Length < 3)
            {
                return null;
            }
            //VERSION COMMAND RSV ATYPE  DST.ADDR  DST.PORT
            //去掉 VERSION COMMAND RSV
           var span = data.Span.Slice(3);

            IPAddress ip = null;
            int index = 0;
            switch ((Socks5EnumAddressType)span[0])
            {
                case Socks5EnumAddressType.IPV4:
                    ip = new IPAddress(span.Slice(1, 4));
                    index = 1 + 4;
                    break;
                case Socks5EnumAddressType.IPV6:
                    ip = new IPAddress(span.Slice(1, 16));
                    index = 1 + 16;
                    break;
                case Socks5EnumAddressType.Domain:
                    ip = NetworkHelper.GetDomainIp(Encoding.UTF8.GetString(span.Slice(2, span[1])));
                    index = 2 + span[1];
                    break;

                default:
                    break;
            }
            ushort int16Port = span.Slice(index, 2).ToUInt16();
            int port = BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(int16Port) : int16Port;

            return new IPEndPoint(ip, port);
        }
        public static Memory<byte> GetUdpData(Memory<byte> span)
        {
            //RSV FRAG ATYPE DST.ADDR DST.PORT DATA
            //去掉 RSV FRAG   RSV占俩字节
            span = span.Slice(3);
            return (Socks5EnumAddressType)span.Span[0] switch
            {
                Socks5EnumAddressType.IPV4 => span[(1 + 4 + 2)..],
                Socks5EnumAddressType.IPV6 => span[(1 + 16 + 2)..],
                Socks5EnumAddressType.Domain => span[(2 + span.Span[1] + 2)..],
                _ => throw new NotImplementedException(),
            };
        }

        public static byte[] MakeConnectResponse(IPEndPoint remoteEndPoint, byte responseCommand)
        {
            //VER REP  RSV ATYPE BND.ADDR BND.PORT

            byte[] ipaddress = remoteEndPoint.Address.GetAddressBytes();
            byte[] port = remoteEndPoint.Port.ToBytes();

            byte[] res = new byte[6 + ipaddress.Length];

            res[0] = 5;
            res[1] = responseCommand;
            res[2] = 0;
            res[3] = (byte)(ipaddress.Length == 4 ? Socks5EnumAddressType.IPV4 : Socks5EnumAddressType.IPV6);

            Array.Copy(ipaddress, 0, res, 4, ipaddress.Length);

            res[res.Length - 2] = port[1];
            res[res.Length - 1] = port[0];

            return res;
        }

        public static Memory<byte> MakeUdpResponse(IPEndPoint remoteEndPoint, Memory<byte> data)
        {
            //RSV FRAG ATYPE DST.ADDR DST.PORT DATA
            //RSV占俩字节

            byte[] ipaddress = remoteEndPoint.Address.GetAddressBytes();
            byte[] port = remoteEndPoint.Port.ToBytes();
            byte[] res = new byte[4 + ipaddress.Length + 2 + data.Length];

            res[0] = 0;
            res[1] = 0;
            res[2] = 0; //FRAG
            res[3] = (byte)(ipaddress.Length == 4 ? Socks5EnumAddressType.IPV4 : Socks5EnumAddressType.IPV6);

            int index = 4;

            Array.Copy(ipaddress, 0, res, index, ipaddress.Length);
            index += ipaddress.Length;

            res[index] = port[1];
            res[index + 1] = port[0];
            index += 2;

            data.CopyTo(res.AsMemory(index, data.Length));

            return res;
        }

    }
}
