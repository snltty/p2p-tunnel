using common.libs.extends;
using System;
using System.ComponentModel;
using System.Net;

namespace common.udpforward
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class UdpForwardRegisterParamsInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public UdpForwardRegisterParamsInfo() { }

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
        /// <returns></returns>
        public byte[] ToBytes()
        {
            byte[] sportBytes = SourcePort.ToBytes();
            byte[] tportBytes = TargetPort.ToBytes();

            byte[] tipBytes = TargetIp.ToBytes();
            byte[] tnameBytes = TargetName.ToBytes();

            byte[] bytes = new byte[2 + 2 + 1 + tipBytes.Length + 1 + tnameBytes.Length];

            int index = 0;

            bytes[index] = sportBytes[0];
            bytes[index + 1] = sportBytes[1];
            index += 2;

            bytes[index] = tportBytes[0];
            bytes[index + 1] = tportBytes[1];
            index += 2;


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

            SourcePort = span.Slice(index, 2).ToUInt16();
            index += 2;

            TargetPort = span.Slice(index, 2).ToUInt16();
            index += 2;

            TargetIp = span.Slice(index + 1, span[index]).GetString();
            index += 1 + span[index];

            TargetName = span.Slice(index + 1, span[index]).GetString();
            index += 1 + span[index];
        }

    }
    /// <summary>
    /// 
    /// </summary>
    public class UdpForwardRegisterResult
    {
        /// <summary>
        /// 
        /// </summary>
        public UdpForwardRegisterResultCodes Code { get; set; } = UdpForwardRegisterResultCodes.OK;
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

            Array.Copy(idBytes, 0, bytes, index, msgBytes.Length);
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

            Code = (UdpForwardRegisterResultCodes)span[index];
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
    public enum UdpForwardRegisterResultCodes : byte
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
