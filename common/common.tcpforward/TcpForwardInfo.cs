using common.libs.extends;
using common.server;
using System;
using System.ComponentModel;

namespace common.tcpforward
{
    /// <summary>
    /// 
    /// </summary>
    public struct ListeningChangeInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public ushort Port { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool State { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class TcpForwardInfo
    {
        /// <summary>
        /// 
        /// </summary>
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
        /// <summary>
        /// 
        /// </summary>
        public Memory<byte> Cache { get; set; }
        /// <summary>
        /// 
        /// </summary>

        [System.Text.Json.Serialization.JsonIgnore]
        public IConnection Connection { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="memory"></param>
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
        Connect = 1,
        /// <summary>
        /// 转发
        /// </summary>
        Forward = 2
    }

    /// <summary>
    /// 
    /// </summary>
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

    /// <summary>
    /// 
    /// </summary>
    [Flags]
    public enum TcpForwardTypes : byte
    {
        /// <summary>
        /// 
        /// </summary>
        [Description("转发")]
        Forward = 1,
        /// <summary>
        /// 
        /// </summary>
        [Description("代理")]
        Proxy = 2
    }

    /// <summary>
    /// 
    /// </summary>
    [Flags]
    public enum TcpForwardAliveTypes : byte
    {
        /// <summary>
        /// 
        /// </summary>
        [Description("长连接")]
        Tunnel = 1,
        /// <summary>
        /// 
        /// </summary>
        [Description("短连接")]
        Web = 2
    }

    /// <summary>
    /// tcp转发相关的消息id
    /// </summary>
    [Flags, MessengerIdEnum]
    public enum TcpForwardMessengerIds : ushort
    {
        /// <summary>
        /// 
        /// </summary>
        Min = 600,
        /// <summary>
        /// 请求
        /// </summary>
        Request = 602,
        /// <summary>
        /// 回执
        /// </summary>
        Response = 603,
        /// <summary>
        /// 获取端口
        /// </summary>
        Ports = 604,
        /// <summary>
        /// 注册
        /// </summary>
        SignIn = 605,
        /// <summary>
        /// 退出
        /// </summary>
        SignOut = 606,
        /// <summary>
        /// 获取配置
        /// </summary>
        GetSetting = 607,
        /// <summary>
        /// 配置
        /// </summary>
        Setting = 608,
        /// <summary>
        /// 
        /// </summary>
        Max = 699,
    }
}
