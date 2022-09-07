using common.libs.extends;
using System;
using System.ComponentModel;
using System.Net;

namespace common.udpforward
{
    public class UdpForwardRegisterParamsInfo
    {
        public UdpForwardRegisterParamsInfo() { }

        public int SourcePort { get; set; } = 8080;
        public string TargetName { get; set; } = string.Empty;
        public string TargetIp { get; set; } = IPAddress.Loopback.ToString();
        public int TargetPort { get; set; } = 8080;
        public UdpForwardTunnelTypes TunnelType { get; set; } = UdpForwardTunnelTypes.TCP_FIRST;

        public byte[] ToBytes()
        {
            byte[] sportBytes = SourcePort.ToBytes();
            byte[] tportBytes = TargetPort.ToBytes();

            byte[] tipBytes = TargetIp.ToBytes();
            byte[] tnameBytes = TargetName.ToBytes();

            byte[] bytes = new byte[1 + 2 + 2 + 1 + tipBytes.Length + 1 + tnameBytes.Length];

            int index = 0;

            bytes[0] = (byte)TunnelType;
            index += 1;

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
        public void DeBytes(Memory<byte> data)
        {
            var span = data.Span;
            int index = 0;

            TunnelType = (UdpForwardTunnelTypes)span[index];
            index += 1;

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

    public class UdpForwardRegisterResult
    {
        public UdpForwardRegisterResultCodes Code { get; set; } = UdpForwardRegisterResultCodes.OK;
        public ulong ID { get; set; } = 0;
        public string Msg { get; set; } = string.Empty;


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

    [Flags]
    public enum UdpForwardRegisterResultCodes : byte
    {
        [Description("成功")]
        OK = 1,
        [Description("插件未开启")]
        DISABLED = 2,
        [Description("已存在")]
        EXISTS = 4,
        [Description("端口超出范围")]
        OUT_RANGE = 8,
        [Description("未知")]
        UNKNOW = 16,
    }
}
