using common.libs;
using common.libs.extends;
using common.server;
using System;
using System.Net;

namespace common.socks5
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class Socks5Info
    {
        /// <summary>
        /// 
        /// </summary>
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
        public uint Id { get; set; } = 0;
        /// <summary>
        /// 来源地址，数据从目标端回来的时候回给谁
        /// </summary>
        public IPEndPoint SourceEP { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public IPEndPoint TargetEP { get; set; }

        /// <summary>
        /// 携带的数据
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public Memory<byte> Data { get; set; } = Helper.EmptyArray;

        /// <summary>
        /// 
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public byte[] Response { get; set; } = new byte[1];
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            byte[] idBytes = Id.ToBytes();
            int length = 1 + idBytes.Length + 1 + 1 + Data.Length, index = 1;
            byte[] ipBytes = Helper.EmptyArray;
            byte[] portBytes = Helper.EmptyArray;
            if (SourceEP != null)
            {
                ipBytes = SourceEP.Address.GetAddressBytes();
                portBytes = SourceEP.Port.ToBytes();
                length += ipBytes.Length + 2;
            }

            byte[] targetipBytes = Helper.EmptyArray;
            byte[] targetportBytes = Helper.EmptyArray;
            if (TargetEP != null)
            {
                targetipBytes = TargetEP.Address.GetAddressBytes();
                targetportBytes = TargetEP.Port.ToBytes();
                length += targetipBytes.Length + 2;
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

            bytes[index] = 0;
            index += 1;
            if (targetipBytes.Length > 0)
            {
                bytes[index - 1] = (byte)(targetipBytes.Length + 2);
                Array.Copy(targetipBytes, 0, bytes, index, targetipBytes.Length);
                index += targetipBytes.Length;

                bytes[index] = targetportBytes[0];
                bytes[index + 1] = targetportBytes[1];
                index += 2;
            }

            if (Data.Length > 0)
            {
                Data.CopyTo(bytes.AsMemory(index));
            }
            return bytes;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        public void DeBytes(Memory<byte> bytes)
        {
            var span = bytes.Span;
            int index = 1;

            Socks5Step = (Socks5EnumStep)(span[0] >> 4);
            Version = (byte)(span[0] & 0b1111);

            Id = span.Slice(index).ToUInt32();
            index += 4;

            byte epLength = span[index];
            index += 1;
            if (epLength > 0)
            {
                IPAddress ip = new IPAddress(span.Slice(index, epLength - 2));
                index += epLength - 2;
                SourceEP = new IPEndPoint(ip, span.Slice(index, 2).ToUInt16());
                index += 2;
            }

            byte targetepLength = span[index];
            index += 1;
            if (targetepLength > 0)
            {
                IPAddress ip = new IPAddress(span.Slice(index, targetepLength - 2));
                index += targetepLength - 2;
                TargetEP = new IPEndPoint(ip, span.Slice(index, 2).ToUInt16());
                index += 2;
            }

            Data = bytes.Slice(index);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
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
        /// <summary>
        /// 
        /// </summary>
        ForwardUdp = 5,
    }

    /// <summary>
    /// socks5的连接地址类型
    /// </summary>
    public enum Socks5EnumAddressType : byte
    {
        /// <summary>
        /// 
        /// </summary>
        IPV4 = 1,
        /// <summary>
        /// 
        /// </summary>
        Domain = 3,
        /// <summary>
        /// 
        /// </summary>
        IPV6 = 4
    }

    /// <summary>
    /// socks5的认证类型
    /// </summary>
    public enum Socks5EnumAuthType : byte
    {
        /// <summary>
        /// 
        /// </summary>
        NoAuth = 0x00,
        /// <summary>
        /// 
        /// </summary>
        GSSAPI = 0x01,
        /// <summary>
        /// 
        /// </summary>
        Password = 0x02,
        /// <summary>
        /// 
        /// </summary>
        IANA = 0x03,
        /// <summary>
        /// 
        /// </summary>
        UnKnow = 0x80,
        /// <summary>
        /// 
        /// </summary>
        NotSupported = 0xff,
    }
    /// <summary>
    /// socks5的认证状态0成功 其它失败
    /// </summary>
    public enum Socks5EnumAuthState : byte
    {
        /// <summary>
        /// 
        /// </summary>
        Success = 0x00,
        /// <summary>
        /// 
        /// </summary>
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
    /// <summary>
    /// socks5相关的消息id
    /// </summary>
    [Flags, MessengerIdEnum]
    public enum Socks5MessengerIds : ushort
    {
        /// <summary>
        /// 
        /// </summary>
        Min = 800,
        /// <summary>
        /// 
        /// </summary>
        Request = 802,
        /// <summary>
        /// 
        /// </summary>
        Response = 803,
        /// <summary>
        /// 
        /// </summary>
        GetSetting = 804,
        /// <summary>
        /// 
        /// </summary>
        Setting = 805,
        /// <summary>
        /// 
        /// </summary>
        Max = 899,
    }

}
