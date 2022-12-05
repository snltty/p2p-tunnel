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
        public string SourceIp { get; set; } = IPAddress.Any.ToString();
        /// <summary>
        /// 
        /// </summary>
        public ushort SourcePort { get; set; } = 8080;
        /// <summary>
        /// 
        /// </summary>
        public string TargetName { get; set; } = string.Empty;
        /// <summary>
        /// 
        /// </summary>
        public string TargetIp { get; set; } = IPAddress.Loopback.ToString();
        /// <summary>
        /// 
        /// </summary>
        public ushort TargetPort { get; set; } = 8080;
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
            byte[] sportBytes = SourcePort.ToBytes();
            byte[] tportBytes = TargetPort.ToBytes();

            byte[] sipBytes = SourceIp.ToBytes();
            byte[] tipBytes = TargetIp.ToBytes();

            byte[] tnameBytes = TargetName.ToBytes();

            byte[] bytes = new byte[1 + 2 + 2 + 1 + sipBytes.Length + 1 + tipBytes.Length + 1 + tnameBytes.Length];

            int index = 0;

            bytes[index] = (byte)AliveType;
            index += 1;

            bytes[index] = sportBytes[0];
            bytes[index + 1] = sportBytes[1];
            index += 2;

            bytes[index] = tportBytes[0];
            bytes[index + 1] = tportBytes[1];
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
        public string SourceIp { get; set; } = IPAddress.Any.ToString();
        /// <summary>
        /// 
        /// </summary>
        public ushort SourcePort { get; set; } = 8080;
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
            byte[] ipBytes = SourceIp.ToBytes();
            byte[] portBytes = SourcePort.ToBytes();
            byte[] bytes = new byte[1 + ipBytes.Length + portBytes.Length];

            int index = 0;

            bytes[index] = (byte)AliveType;
            index += 1;

            bytes[index] = portBytes[0];
            bytes[index + 1] = portBytes[1];
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
        public TcpForwardRegisterResultCodes Code { get; set; } = TcpForwardRegisterResultCodes.OK;
        /// <summary>
        /// 
        /// </summary>
        public ulong ID { get; set; } = 0;
        /// <summary>
        /// 
        /// </summary>
        public string Msg { get; set; } = string.Empty;
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            var idBytes = ID.ToBytes();
            var msgBytes = Msg.ToBytes();
            var bytes = new byte[1 + 8 + 1 + msgBytes.Length];

            int index = 0;

            bytes[index] = (byte)Code;
            index += 1;

            Array.Copy(idBytes, 0, bytes, index, idBytes.Length);
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
        OK = 1,
        /// <summary>
        /// 
        /// </summary>
        [Description("插件未开启")]
        DISABLED = 2,
        /// <summary>
        /// 
        /// </summary>
        [Description("已存在")]
        EXISTS = 4,
        /// <summary>
        /// 
        /// </summary>
        [Description("端口超出范围")]
        OUT_RANGE = 8,
        /// <summary>
        /// 
        /// </summary>
        [Description("未知")]
        UNKNOW = 16,
    }
}
