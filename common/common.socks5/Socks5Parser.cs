using common.libs;
using common.libs.extends;
using System;
using System.Buffers.Binary;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace common.socks5
{
    /// <summary>
    /// socks5 数据包解析和组装
    /// </summary>
    public sealed class Socks5Parser
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
            byte length = span[1];
            Socks5EnumAuthType[] res = new Socks5EnumAuthType[length];
            for (byte i = 0; i < length; i++)
            {
                res[i] = (Socks5EnumAuthType)span[2 + i];
            }
            return res;
        }
        /// <summary>
        /// 获取账号密码
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 获取地址
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static IPEndPoint GetRemoteEndPoint(Memory<byte> data)
        {
            try
            {
                if (data.Length <= 4)
                {
                    return new IPEndPoint(IPAddress.Any, 0);
                }
                //VERSION COMMAND RSV ATYPE  DST.ADDR  DST.PORT
                //去掉 VERSION COMMAND RSV
                var span = data.Span.Slice(3);

                IPAddress ip = IPAddress.Any;
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
                        {
                            try
                            {
                                ip = NetworkHelper.GetDomainIp(Encoding.UTF8.GetString(span.Slice(2, span[1])));
                                index = 2 + span[1];
                            }
                            catch (Exception ex)
                            {
                                Logger.Instance.DebugError(string.Join(",", span.ToArray()));
                                Logger.Instance.DebugError(Encoding.UTF8.GetString(span));
                                Logger.Instance.DebugError(Encoding.UTF8.GetString(span.Slice(2, span[1])));
                                Logger.Instance.DebugError(ex);
                                return new IPEndPoint(IPAddress.Any, 0);
                            }
                        }
                        break;

                    default:
                        break;
                }
                ushort int16Port = span.Slice(index, 2).ToUInt16();
                int port = BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(int16Port) : int16Port;

                return new IPEndPoint(ip, port);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
            return new IPEndPoint(IPAddress.Any, 0);
        }
        /// <summary>
        /// 获取地址
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static IPAddress GetRemoteAddress(Memory<byte> data)
        {
            try
            {
                if (data.Length <= 4)
                {
                    return IPAddress.Any;
                }
                //VERSION COMMAND RSV ATYPE  DST.ADDR  DST.PORT
                //去掉 VERSION COMMAND RSV
                var span = data.Span.Slice(3);
                return (Socks5EnumAddressType)span[0] switch
                {
                    Socks5EnumAddressType.IPV4 => new IPAddress(span.Slice(1, 4)),
                    Socks5EnumAddressType.IPV6 => new IPAddress(span.Slice(1, 16)),
                    Socks5EnumAddressType.Domain => NetworkHelper.GetDomainIp(Encoding.UTF8.GetString(span.Slice(2, span[1]))),
                    _ => IPAddress.Any,
                };
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
            return IPAddress.Any;
        }

        /// <summary>
        /// 是否是 任意地址
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static bool GetIsAnyAddress(Memory<byte> data)
        {
            var span = data.Span.Slice(3);
            return ((Socks5EnumAddressType)span[0]) switch
            {
                Socks5EnumAddressType.IPV4 => span.Slice(1, 4).SequenceEqual(Helper.AnyIpArray) || span.Slice(1 + 4, 2).SequenceEqual(Helper.AnyPoryArray),
                Socks5EnumAddressType.IPV6 => span.Slice(1, 16).SequenceEqual(Helper.AnyIpv6Array) || span.Slice(1 + 16, 2).SequenceEqual(Helper.AnyPoryArray),
                Socks5EnumAddressType.Domain => false || span.Slice(2 + span[1], 2).SequenceEqual(Helper.AnyPoryArray),
                _ => false,
            };
        }
        /// <summary>
        /// 是否是局域网地址
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static bool GetIsLanAddress(Memory<byte> data)
        {
            var memory = data.Slice(3);
            var span = memory.Span;
            return ((Socks5EnumAddressType)span[0]) switch
            {
                Socks5EnumAddressType.IPV4 => memory.Slice(1, 4).IsLan(),
                Socks5EnumAddressType.IPV6 => memory.Slice(1, 16).IsLan(),
                Socks5EnumAddressType.Domain => false,
                _ => false,
            };
        }

        /// <summary>
        /// 是否是广播地址
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static bool GetIsBroadcastAddress(Memory<byte> data)
        {
            var span = data.Span.Slice(3);
            if ((Socks5EnumAddressType)span[0] == Socks5EnumAddressType.IPV4)
            {
                span = span.Slice(1, 4);
                uint ip = Unsafe.As<byte, uint>(ref MemoryMarshal.GetReference(span));
                return ip >= 0xFF0000E0 && ip <= 0xFFFFFFEF;
            }
            return false;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="remoteEndPoint"></param>
        /// <param name="responseCommand"></param>
        /// <returns></returns>
        public static unsafe byte[] MakeConnectResponse(IPEndPoint remoteEndPoint, byte responseCommand)
        {
            //VER REP  RSV ATYPE BND.ADDR BND.PORT

            byte[] res = new byte[6 + remoteEndPoint.Address.Length()];
            var span = res.AsSpan();

            res[0] = 5;
            res[1] = responseCommand;
            res[2] = 0;
            res[3] = (byte)(remoteEndPoint.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? Socks5EnumAddressType.IPV4 : Socks5EnumAddressType.IPV6);

            remoteEndPoint.Address.TryWriteBytes(span.Slice(4), out _);

            int port = remoteEndPoint.Port;
            ref int _port = ref port;
            fixed (void* p = &_port)
            {
                byte* pp = (byte*)p;
                res[^2] = *(pp + 1);
                res[^1] = *pp;
            }


            return res;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="remoteEndPoint"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static unsafe Memory<byte> MakeUdpResponse(IPEndPoint remoteEndPoint, Memory<byte> data)
        {
            //RSV FRAG ATYPE DST.ADDR DST.PORT DATA
            //RSV占俩字节

            int ipLength = remoteEndPoint.Address.Length();
            byte[] res = new byte[4 + ipLength + 2 + data.Length];
            var span = res.AsSpan();

            res[0] = 0;
            res[1] = 0;
            res[2] = 0; //FRAG
            res[3] = (byte)(ipLength == 4 ? Socks5EnumAddressType.IPV4 : Socks5EnumAddressType.IPV6);

            int index = 4;

            remoteEndPoint.Address.TryWriteBytes(span.Slice(index), out _);
            index += ipLength;

            int port = remoteEndPoint.Port;
            ref int _port = ref port;
            fixed (void* p = &_port)
            {
                byte* pp = (byte*)p;
                res[index] = *(pp + 1);
                res[index + 1] = *pp;
            }
            index += 2;

            data.CopyTo(res.AsMemory(index, data.Length));

            return res;
        }


        /// <summary>
        /// 验证 request数据完整性
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static bool ValidateRequestData(Memory<byte> data, out bool gt)
        {
            gt = false;
            if (data.Length > 2)
            {
                gt = data.Length > 2 + data.Span[1];
                return data.Length == 2 + data.Span[1];
            }
            return false;
        }
        /// <summary>
        /// 验证command数据完整性
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static bool ValidateCommandData(Memory<byte> data, out bool gt)
        {
            gt = false;
            /*
             * VERSION  COMMAND RSV ADDRESS_TYPE    DST.ADDR    DST.PORT
             * 1        1       1   1               1-255       2
             * 域名模式下 DST.ADDR第一个字节是域名长度，那么整个数据至少5个字节
             */
            if (data.Length < 5) return false;

            var span = data.Span;
            int addrLength = (Socks5EnumAddressType)span[3] switch
            {
                Socks5EnumAddressType.IPV4 => 4 + 2,
                Socks5EnumAddressType.Domain => span[4] + 1 + 2, //DST.ADDR第一个字节是域名长度 剩下的才是域名数据
                Socks5EnumAddressType.IPV6 => 16 + 2,
                _ => throw new NotImplementedException(),
            };
            gt = data.Length > 4 + addrLength;
            //首部4字节+地址长度
            return data.Length == 4 + addrLength;
        }

        /// <summary>
        /// 验证认证数据完整性
        /// </summary>
        /// <param name="data"></param>
        /// <param name="authType"></param>
        /// <param name="gt"></param>
        /// <returns></returns>
        public static bool ValidateAuthData(Memory<byte> data, Socks5EnumAuthType authType, out bool gt)
        {
            gt = false;
            return authType switch
            {
                Socks5EnumAuthType.NoAuth => true,
                Socks5EnumAuthType.Password => ValidateAuthPasswordData(data, out gt),
                Socks5EnumAuthType.GSSAPI => true,
                Socks5EnumAuthType.IANA => true,
                Socks5EnumAuthType.UnKnow => false,
                Socks5EnumAuthType.NotSupported => false,
                _ => false,
            };
        }
        private static bool ValidateAuthPasswordData(Memory<byte> data, out bool gt)
        {
            /*
             VERSION	USERNAME_LENGTH	USERNAME	PASSWORD_LENGTH	PASSWORD
                1字节	1字节	        1到255字节	1字节	        1到255字节
                0x01	0x01	        0x0a	    0x01	        0x0a
             */
            gt = false;

            var span = data.Slice(1).Span;
            //至少有 USERNAME_LENGTH  PASSWORD_LENGTH 字节以上
            if (span.Length <= 2)
            {
                return false;
            }

            byte nameLength = span[0];
            //至少有 USERNAME_LENGTH USERNAME  PASSWORD_LENGTH
            if (span.Length < nameLength + 1 + 1)
            {
                return false;
            }

            byte passwordLength = span[1 + nameLength];

            gt = span.Length > 1 + 1 + nameLength + passwordLength;
            return span.Length == 1 + 1 + nameLength + passwordLength;
        }
    }
}
