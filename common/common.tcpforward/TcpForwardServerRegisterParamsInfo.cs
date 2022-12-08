using common.libs.extends;
using System;
using System.ComponentModel;
using System.Net;

namespace common.tcpforward
{
    /// <summary>
    /// Tcp转发
    /// </summary>
    public sealed class TcpForwardRegisterParamsInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public TcpForwardRegisterParamsInfo() { }

        /// <summary>
        /// 
        /// </summary>
        public string SourceIp { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public ushort SourcePort { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string TargetName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string TargetIp { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public ushort TargetPort { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public TcpForwardAliveTypes AliveType { get; set; } = TcpForwardAliveTypes.Web;
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            byte[] sipBytes = SourceIp.ToBytes();
            byte[] tipBytes = TargetIp.ToBytes();
            byte[] tnameBytes = TargetName.ToBytes();

            byte[] bytes = new byte[
                1  //AliveType
                + 2  //SourcePort
                + 2  //TargetPort
                + 1 + sipBytes.Length  //SourceIp
                + 1 + tipBytes.Length  //TargetIp
                + 1 + tnameBytes.Length //TargetName
             ];
            var memory = bytes.AsMemory();

            int index = 0;

            bytes[index] = (byte)AliveType;
            index += 1;

            SourcePort.ToBytes(memory.Slice(index));
            index += 2;

            TargetPort.ToBytes(memory.Slice(index));
            index += 2;

            bytes[index] = (byte)sipBytes.Length;
            Array.Copy(sipBytes, 0, bytes, index + 1, sipBytes.Length);
            index += 1 + sipBytes.Length;

            bytes[index] = (byte)tipBytes.Length;
            Array.Copy(tipBytes, 0, bytes, index + 1, tipBytes.Length);
            index += 1 + tipBytes.Length;

            bytes[index] = (byte)tnameBytes.Length;
            Array.Copy(tnameBytes, 0, bytes, index + 1, tnameBytes.Length);
            index += 1 + tnameBytes.Length;

            return bytes;

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public void DeBytes(Memory<byte> data)
        {
            var span = data.Span;
            int index = 0;

            AliveType = (TcpForwardAliveTypes)span[index];
            index += 1;

            SourcePort = span.Slice(index, 2).ToUInt16();
            index += 2;

            TargetPort = span.Slice(index, 2).ToUInt16();
            index += 2;

            SourceIp = span.Slice(index + 1, span[index]).GetString();
            index += 1 + span[index];

            TargetIp = span.Slice(index + 1, span[index]).GetString();
            index += 1 + span[index];

            TargetName = span.Slice(index + 1, span[index]).GetString();
            index += 1 + span[index];
        }

    }
    /// <summary>
    /// 
    /// </summary>
    public class TcpForwardUnRegisterParamsInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public TcpForwardUnRegisterParamsInfo() { }
        /// <summary>
        /// 
        /// </summary>
        public string SourceIp { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public ushort SourcePort { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public TcpForwardAliveTypes AliveType { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            byte[] ipBytes = SourceIp.ToBytes();
            byte[] bytes = new byte[1 + ipBytes.Length + 2];

            int index = 0;

            bytes[index] = (byte)AliveType;
            index += 1;

            SourcePort.ToBytes(bytes.AsMemory(index));
            index += 2;

            Array.Copy(ipBytes, 0, bytes, index, ipBytes.Length);
            index += ipBytes.Length;

            return bytes;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public void DeBytes(Memory<byte> data)
        {
            var span = data.Span;
            int index = 0;

            AliveType = (TcpForwardAliveTypes)span[index];
            index += 1;

            SourcePort = span.Slice(index, 2).ToUInt16();
            index += 2;

            SourceIp = span.Slice(index).GetString();
        }
    }
    /// <summary>
    /// 
    /// </summary>
    public class TcpForwardRegisterResult
    {
        /// <summary>
        /// 
        /// </summary>
        public TcpForwardRegisterResultCodes Code { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public ulong ID { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Msg { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            var msgBytes = Msg.ToBytes();
            var bytes = new byte[
                1
                + 8
                + 1 + msgBytes.Length];

            int index = 0;

            bytes[index] = (byte)Code;
            index += 1;

            ID.ToBytes(bytes.AsMemory(index));
            index += 8;

            bytes[index] = (byte)msgBytes.Length;
            index += 1;
            Array.Copy(msgBytes, 0, bytes, index, msgBytes.Length);

            return bytes;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public void DeBytes(ReadOnlyMemory<byte> data)
        {
            var span = data.Span;
            int index = 0;

            Code = (TcpForwardRegisterResultCodes)span[index];
            index += 1;

            ID = span.Slice(index, 8).ToUInt64();
            index += 8;

            Msg = span.Slice(index + 1, span[index]).GetString();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [Flags]
    public enum TcpForwardRegisterResultCodes : byte
    {
        /// <summary>
        /// 
        /// </summary>
        [Description("成功")]
        OK = 0,
        /// <summary>
        /// 
        /// </summary>
        [Description("插件未开启")]
        DISABLED = 1,
        /// <summary>
        /// 
        /// </summary>
        [Description("已存在")]
        EXISTS = 2,
        /// <summary>
        /// 
        /// </summary>
        [Description("端口超出范围")]
        OUT_RANGE = 4,
        /// <summary>
        /// 
        /// </summary>
        [Description("未知")]
        UNKNOW = 8,
    }
}
