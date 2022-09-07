using common.libs;
using common.libs.extends;
using System;
using System.Net;

namespace common.socks5
{
    public class Socks5Info
    {
        public object Tag { get; set; }

        /// <summary>
        /// socks5步骤，执行到哪一步了
        /// </summary>
        public Socks5EnumStep Socks5Step { get; set; } = Socks5EnumStep.Request;
        /// <summary>
        /// 版本
        /// </summary>
        public byte Version { get; set; } = 0;
        /// <summary>
        /// 请求id
        /// </summary>
        public ulong Id { get; set; } = 0;
        /// <summary>
        /// 来源地址，数据从目标端回来的时候回给谁
        /// </summary>
        public IPEndPoint SourceEP { get; set; }

        /// <summary>
        /// 携带的数据
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public Memory<byte> Data { get; set; } = Helper.EmptyArray;

        [System.Text.Json.Serialization.JsonIgnore]
        public byte[] Response { get; set; } = new byte[1];
        public byte[] ToBytes()
        {
            byte[] idBytes = Id.ToBytes();
            int length = 1 + idBytes.Length + 1 + Data.Length, index = 1;
            byte[] ipBytes = Helper.EmptyArray;
            byte[] portBytes = Helper.EmptyArray;
            if (SourceEP != null)
            {
                ipBytes = SourceEP.Address.GetAddressBytes();
                portBytes = SourceEP.Port.ToBytes();
                length += ipBytes.Length + 2;
            }

            byte[] bytes = new byte[length];
            bytes[0] = (byte)((byte)Socks5Step << 4 | Version);

            Array.Copy(idBytes, 0, bytes, index, idBytes.Length);
            index += idBytes.Length;

            bytes[index] = 0;
            index += 1;
            if (ipBytes.Length > 0)
            {
                bytes[index - 1] = (byte)(ipBytes.Length + 2);
                Array.Copy(ipBytes, 0, bytes, index, ipBytes.Length);
                index += ipBytes.Length;

                bytes[index] = portBytes[0];
                bytes[index + 1] = portBytes[1];
                index += 2;
            }

            if (Data.Length > 0)
            {
                Data.CopyTo(bytes.AsMemory(index));
            }
            return bytes;
        }

        public void DeBytes(Memory<byte> bytes)
        {
            var span = bytes.Span;
            int index = 1;

            Socks5Step = (Socks5EnumStep)(span[0] >> 4);
            Version = (byte)(span[0] & 0b1111);

            Id = span.Slice(index, 8).ToUInt64();
            index += 8;

            byte epLength = span[index];
            index += 1;
            if (epLength > 0)
            {
                IPAddress ip = new IPAddress(span.Slice(index, epLength - 2));
                index += epLength - 2;
                SourceEP = new IPEndPoint(ip, span.Slice(index, 2).ToUInt16());
                index += 2;
            }

            Data = bytes.Slice(index);
        }

        public static Socks5Info Debytes(Memory<byte> data)
        {
            Socks5Info info = new Socks5Info();
            info.DeBytes(data);
            return info;
        }
    }
    /// <summary>
    /// 当前处于socks5协议的哪一步
    /// </summary>
    public enum Socks5EnumStep : byte
    {
        /// <summary>
        /// 第一次请求，处理认证方式
        /// </summary>
        Request = 1,
        /// <summary>
        /// 如果有认证
        /// </summary>
        Auth = 2,
        /// <summary>
        /// 发送命令，CONNECT BIND 还是  UDP ASSOCIATE
        /// </summary>
        Command = 3,
        /// <summary>
        /// 转发
        /// </summary>
        Forward = 4,
        ForwardUdp = 5,
    }

    /// <summary>
    /// socks5的连接地址类型
    /// </summary>
    public enum Socks5EnumAddressType : byte
    {
        IPV4 = 1,
        Domain = 3,
        IPV6 = 4
    }

    /// <summary>
    /// socks5的认证类型
    /// </summary>
    public enum Socks5EnumAuthType : byte
    {
        NoAuth = 0x00,
        GSSAPI = 0x01,
        Password = 0x02,
        IANA = 0x03,
        UnKnow = 0x80,
        NotSupported = 0xff,
    }
    /// <summary>
    /// socks5的认证状态0成功 其它失败
    /// </summary>
    public enum Socks5EnumAuthState : byte
    {
        Success = 0x00,
        UnKnow = 0xff,
    }
    /// <summary>
    /// socks5的请求指令
    /// </summary>
    public enum Socks5EnumRequestCommand : byte
    {
        /// <summary>
        /// 连接上游服务器
        /// </summary>
        Connect = 1,
        /// <summary>
        /// 绑定，客户端会接收来自代理服务器的链接，著名的FTP被动模式
        /// </summary>
        Bind = 2,
        /// <summary>
        /// UDP中继
        /// </summary>
        UdpAssociate = 3
    }
    /// <summary>
    /// socks5的请求的回复数据的指令
    /// </summary>
    public enum Socks5EnumResponseCommand : byte
    {
        /// <summary>
        /// 代理服务器连接目标服务器成功
        /// </summary>
        ConnecSuccess = 0,
        /// <summary>
        /// 代理服务器故障
        /// </summary>
        ServerError = 1,
        /// <summary>
        /// 代理服务器规则集不允许连接
        /// </summary>
        ConnectNotAllow = 2,
        /// <summary>
        /// 网络无法访问
        /// </summary>
        NetworkError = 3,
        /// <summary>
        /// 目标服务器无法访问（主机名无效）
        /// </summary>
        ConnectFail = 4,
        /// <summary>
        /// 连接目标服务器被拒绝
        /// </summary>
        DistReject = 5,
        /// <summary>
        /// TTL已过期
        /// </summary>
        TTLTimeout = 6,
        /// <summary>
        /// 不支持的命令
        /// </summary>
        CommandNotAllow = 7,
        /// <summary>
        /// 不支持的目标服务器地址类型
        /// </summary>
        AddressNotAllow = 8,
        /// <summary>
        /// 未分配
        /// </summary>
        Unknow = 8,
    }

}
