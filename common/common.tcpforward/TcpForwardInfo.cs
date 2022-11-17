using common.libs.extends;
using common.server;
using System;
using System.ComponentModel;

namespace common.tcpforward
{
    public struct ListeningChangeInfo
    {
        public ushort Port { get; set; }
        public bool State { get; set; }
    }

    public class TcpForwardInfo
    {
        public TcpForwardInfo() { }
        /// <summary>
        /// 监听的端口
        /// </summary>
        public ushort SourcePort { get; set; }

        /// <summary>
        /// 短链接还是长连接
        /// </summary>
        public TcpForwardAliveTypes AliveType { get; set; }

        /// <summary>
        /// 转发类型，是转发还是代理
        /// </summary>
        public TcpForwardTypes ForwardType { get; set; }

        /// <summary>
        /// 数据类型，步骤
        /// </summary>
        public TcpForwardDataTypes DataType { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        public TcpForwardStateTypes StateType { get; set; }

        /// <summary>
        /// 请求id
        /// </summary>
        public uint RequestId { get; set; }

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
        public Memory<byte> Cache { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public IConnection Connection { get; set; }

        public byte[] ToBytes()
        {
            /*
                连接类型  转发类型  数据类型  状态类型  请求id   目标地址  数据 

                  1bit    1bit     4bit     4bit    8byte     1+any  any
             */


            byte[] requestIdBytes = RequestId.ToBytes();

            int length =
                1 +  //AliveType + ForwardType
                1 + // DataType + StateType
                 requestIdBytes.Length +
                1 + TargetEndpoint.Length +
                Buffer.Length;

            byte[] bytes = new byte[length];
            int index = 0;


            bytes[index] = (byte)(((byte)AliveType - 1) << 1 | ((byte)ForwardType - 1));
            index += 1;

            bytes[index] = (byte)((byte)DataType << 4 | (byte)StateType);
            index += 1;

            Array.Copy(requestIdBytes, 0, bytes, index, requestIdBytes.Length);
            index += requestIdBytes.Length;

            bytes[index] = (byte)TargetEndpoint.Length;
            index += 1;
            TargetEndpoint.CopyTo(bytes.AsMemory(index, TargetEndpoint.Length));
            index += TargetEndpoint.Length;

            Buffer.CopyTo(bytes.AsMemory(index, Buffer.Length));
            index += Buffer.Length;
            return bytes;
        }

        public void DeBytes(in Memory<byte> memory)
        {
            var span = memory.Span;
            int index = 0;

            AliveType = (TcpForwardAliveTypes)(byte)(((span[index] >> 1) & 0b1) + 1);
            ForwardType = (TcpForwardTypes)(byte)((span[index] & 0b1) + 1);
            index += 1;

            DataType = (TcpForwardDataTypes)(byte)((span[index] >> 4) & 0b1111);
            StateType = (TcpForwardStateTypes)(byte)(span[index] & 0b1111);
            index += 1;

            RequestId = span.Slice(index).ToUInt32();
            index += 4;

            byte epLength = span[index];
            index += 1;
            TargetEndpoint = memory.Slice(index, epLength);
            index += epLength;



            Buffer = memory.Slice(index);
        }
    }

    /// <summary>
    /// 数据类型，也是步骤
    /// </summary>
    [Flags]
    public enum TcpForwardDataTypes : byte
    {
        /// <summary>
        /// 连接，
        /// </summary>
        CONNECT = 1,
        /// <summary>
        /// 转发
        /// </summary>
        FORWARD = 2
    }

    [Flags]
    public enum TcpForwardStateTypes : byte
    {
        /// <summary>
        /// 成功，
        /// </summary>
        Success = 1,
        /// <summary>
        /// 失败
        /// </summary>
        Fail = 2,
        /// <summary>
        /// 关闭
        /// </summary>
        Close = 4
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

    [Flags, MessengerIdEnum]
    public enum TcpForwardMessengerIds : ushort
    {
        Min = 601,
        Request = 602,
        Response = 603,
        Ports = 604,
        SignIn = 605,
        SignOut = 606,
        Max = 700,
    }
}
