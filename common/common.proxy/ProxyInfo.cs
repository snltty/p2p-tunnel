using common.libs.extends;
using common.server;
using common.server.model;
using System;
using System.Buffers;
using System.Net;

namespace common.proxy
{
    public sealed class ProxyInfo
    {
        #region 数据字段

        public byte Rsv { get; set; }
        /// <summary>
        /// 执行到哪一步了
        /// </summary>
        public EnumProxyStep Step { get; set; } = EnumProxyStep.Command;
        /// <summary>
        /// 连接类型
        /// </summary>
        public EnumProxyCommand Command { get; set; } = EnumProxyCommand.Connect;
        /// <summary>
        /// 地址类型
        /// </summary>
        public EnumProxyAddressType AddressType { get; set; } = EnumProxyAddressType.IPV4;
        /// <summary>
        /// buffer size
        /// </summary>
        public EnumBufferSize BufferSize { get; set; } = EnumBufferSize.KB_8;
        /// <summary>
        /// 插件id
        /// </summary>
        public byte PluginId { get; set; }

        /// <summary>
        /// 请求id
        /// </summary>
        public uint RequestId { get; set; }
        /// <summary>
        /// 来源地址，数据从目标端回来的时候回给谁
        /// </summary>
        public IPEndPoint SourceEP { get; set; }
        /// <summary>
        /// 目标地址
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public Memory<byte> TargetAddress { get; set; }
        public ushort TargetPort { get; set; }

        /// <summary>
        /// 携带的数据
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public Memory<byte> Data { get; set; }

        #endregion

        #region 辅助字段

        /// <summary>
        /// 监听的端口
        /// </summary>
        public ushort ListenPort { get; set; }
        /// <summary>
        /// 连接对象，在发送端表示目标连接，在接收端表示来源连接
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public IConnection Connection { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public byte[] Response { get; set; } = new byte[1];
        [System.Text.Json.Serialization.JsonIgnore]
        public IProxyPlugin ProxyPlugin { get; set; }

        #endregion

        public byte[] ToBytes(out int length)
        {
            length = 1 //0000 0000  rsv + step
                + 1 // 0000 0000 command + address type
                + 1 //buffer size
                + 1 //PluginId
                + 4  // RequestId
                + 1  //source length
                + 1 // target
                + Data.Length;

            int sepLength = 0;
            if (SourceEP != null)
            {
                sepLength = SourceEP.Address.Length();
                length += sepLength + 2;
            }
            if (TargetAddress.Length > 0)
            {
                length += TargetAddress.Length + 2;
            }

            byte[] bytes = ArrayPool<byte>.Shared.Rent(length);
            var memory = bytes.AsMemory(0, length);
            var span = memory.Span;
            int index = 0;


            bytes[index] = (byte)((Rsv << 4) | ((byte)Step));
            index += 1;
            bytes[index] = (byte)(((byte)Command << 4) | (byte)AddressType);
            index += 1;
            bytes[index] = (byte)BufferSize;
            index += 1;
            bytes[index] = PluginId;
            index += 1;

            RequestId.ToBytes(memory.Slice(index));
            index += 4;

            bytes[index] = (byte)sepLength;
            index += 1;
            if (sepLength > 0)
            {
                SourceEP.Address.TryWriteBytes(span.Slice(index), out _);
                index += sepLength;

                ((ushort)SourceEP.Port).ToBytes(memory.Slice(index));
                index += 2;
            }

            bytes[index] = (byte)TargetAddress.Length;
            index += 1;
            if (TargetAddress.Length > 0)
            {
                TargetAddress.CopyTo(memory.Slice(index));
                index += TargetAddress.Length;
                TargetPort.ToBytes(memory.Slice(index));
                index += 2;
            }


            if (Data.Length > 0)
            {
                Data.CopyTo(memory.Slice(index));
            }
            return bytes;
        }

        public void DeBytes(Memory<byte> bytes)
        {
            var span = bytes.Span;
            int index = 0;

            Rsv = (byte)(span[index] >> 4);
            Step = (EnumProxyStep)(span[index] & 0b0000_1111);
            index++;

            Command = (EnumProxyCommand)(span[index] >> 4);
            AddressType = (EnumProxyAddressType)(span[index] & 0b0000_1111);
            index += 1;

            BufferSize = (EnumBufferSize)span[index];
            index += 1;
            PluginId = span[index];
            index += 1;

            RequestId = span.Slice(index).ToUInt32();
            index += 4;

            byte epLength = span[index];
            index += 1;
            if (epLength > 0)
            {
                IPAddress ip = new IPAddress(span.Slice(index, epLength));
                index += epLength;
                SourceEP = new IPEndPoint(ip, span.Slice(index, 2).ToUInt16());
                index += 2;
            }

            byte targetepLength = span[index];
            index += 1;
            if (targetepLength > 0)
            {
                TargetAddress = bytes.Slice(index, targetepLength);
                index += targetepLength;
                TargetPort = span.Slice(index, 2).ToUInt16();
                index += 2;
            }

            Data = bytes.Slice(index);
        }

        public static ProxyInfo Debytes(Memory<byte> data)
        {
            ProxyInfo info = new ProxyInfo();
            info.DeBytes(data);
            return info;
        }

        public void Return(byte[] data)
        {
            ArrayPool<byte>.Shared.Return(data);
        }

    }

    /// <summary>
    /// 当前处于协议的哪一步， 2位
    /// </summary>
    public enum EnumProxyStep : byte
    {
        Command = 1,
        /// <summary>
        /// TCP转发
        /// </summary>
        ForwardTcp = 2,
        /// <summary>
        /// UDP转发
        /// </summary>
        ForwardUdp = 3
    }

    /// <summary>
    /// 连接地址类型，3位
    /// </summary>
    public enum EnumProxyAddressType : byte
    {
        IPV4 = 1,
        Domain = 3,
        IPV6 = 4
    }

    /// <summary>
    /// 请求指令 2位
    /// </summary>
    public enum EnumProxyCommand : byte
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
    /// 请求的回复数据的指令
    /// </summary>
    public enum EnumProxyCommandStatus : byte
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
    /// 数据验证结果
    /// </summary>
    [Flags]
    public enum EnumProxyValidateDataResult : byte
    {
        Equal = 1,
        TooShort = 2,
        TooLong = 4,
        Bad = 8,
    }

    /// <summary>
    /// 代理消息编号
    /// </summary>
    [Flags]
    public enum ProxyMessengerIds : ushort
    {
        Min = 900,
        Request = 901,
        Response = 902,
        Max = 999,
    }
}
