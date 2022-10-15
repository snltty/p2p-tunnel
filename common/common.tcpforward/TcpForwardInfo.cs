using common.libs.extends;
using common.server;
using System;
using System.ComponentModel;

namespace common.tcpforward
{
    public struct ListeningChangeInfo
    {
        public int Port { get; set; }
        public bool State { get; set; }
    }

    public class TcpForwardInfo
    {
        public TcpForwardInfo() { }
        /// <summary>
        /// 监听的端口
        /// </summary>
        public int SourcePort { get; set; }

        /// <summary>
        /// 是否是转发数据阶段，连接阶段需要带上TargetEndpoint，而转发阶段不需要待
        /// </summary>
        public bool IsForward { get; set; }
        /// <summary>
        /// 短链接还是长连接
        /// </summary>
        public TcpForwardAliveTypes AliveType { get; set; }
        /// <summary>
        /// 转发类型，是转发还是代理
        /// </summary>
        public TcpForwardTypes ForwardType { get; set; }
        /// <summary>
        /// 请求id
        /// </summary>
        public ulong RequestId { get; set; }

        /// <summary>
        /// 目标地址
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public Memory<byte> TargetEndpoint { get; set; }

        /// <summary>
        /// 转发的数据
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public Memory<byte> Buffer { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public IConnection Connection { get; set; }

        public byte[] ToBytes()
        {
            byte[] requestIdBytes = RequestId.ToBytes();
            int length = 1 + 1 + requestIdBytes.Length + Buffer.Length;
            length += TargetEndpoint.Length;

            byte[] bytes = new byte[length];
            int index = 2;
            /*
                0b111  
                AliveType               1bit
                ForwardType             1bit
                isForward               1bit
             */
            bytes[0] |= (byte)(((byte)AliveType - 1) << 2);
            bytes[0] |= (byte)(((byte)ForwardType - 1) << 1);
            bytes[0] |= 0;
            bytes[1] = (byte)TargetEndpoint.Length;

            TargetEndpoint.CopyTo(bytes.AsMemory(index, TargetEndpoint.Length));
            index += TargetEndpoint.Length;

            Array.Copy(requestIdBytes, 0, bytes, index, requestIdBytes.Length);
            index += requestIdBytes.Length;

            Buffer.CopyTo(bytes.AsMemory(index, Buffer.Length));
            index += Buffer.Length;
            return bytes;
        }

        public void DeBytes(in Memory<byte> memory)
        {
            var span = memory.Span;
            int index = 2;

            //转发阶段不需要这些
            AliveType = (TcpForwardAliveTypes)(byte)(((span[0] >> 2) & 0b1) + 1);
            ForwardType = (TcpForwardTypes)(byte)(((span[0] >> 1) & 0b1) + 1);
            IsForward = (span[0] & 0b1) == 1;
            byte epLength = span[1];

            TargetEndpoint = memory.Slice(index, epLength);
            index += epLength;

            RequestId = span.Slice(index).ToUInt64();
            index += 8;

            Buffer = memory.Slice(index);
        }
    }

    [Flags]
    public enum TcpForwardTypes : byte
    {
        [Description("转发")]
        FORWARD = 1,
        [Description("代理")]
        PROXY = 2
    }

    [Flags]
    public enum TcpForwardAliveTypes : byte
    {
        [Description("长连接")]
        TUNNEL = 1,
        [Description("短连接")]
        WEB = 2
    }

    [Flags]
    public enum TcpForwardTunnelTypes : byte
    {
        [Description("只tcp")]
        TCP = 1 << 1,
        [Description("只udp")]
        UDP = 1 << 2,
        [Description("优先tcp")]
        TCP_FIRST = 1 << 3,
        [Description("优先udp")]
        UDP_FIRST = 1 << 4,
    }
}
