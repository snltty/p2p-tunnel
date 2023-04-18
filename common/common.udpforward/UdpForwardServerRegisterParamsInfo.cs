using common.libs.extends;
using System;
using System.ComponentModel;
using System.Net;

namespace common.udpforward
{
    public sealed class UdpForwardRegisterParamsInfo
    {
        public UdpForwardRegisterParamsInfo() { }

        public ushort SourcePort { get; set; }
        public string TargetIp { get; set; }
        public ushort TargetPort { get; set; }

        public byte[] ToBytes()
        {
            var tipBytes = TargetIp.GetUTF16Bytes();

            byte[] bytes = new byte[
                2 + 2
                + 1 + 1 + tipBytes.Length
            ];
            var memory = bytes.AsMemory();
            var span = bytes.AsSpan();

            int index = 0;

            SourcePort.ToBytes(memory.Slice(index));
            index += 2;

            TargetPort.ToBytes(memory.Slice(index));
            index += 2;


            bytes[index] = (byte)tipBytes.Length;
            index += 1;
            bytes[index] = (byte)TargetIp.Length;
            index += 1;
            tipBytes.CopyTo(span.Slice(index));
            index += tipBytes.Length;

            return bytes;

        }

        public void DeBytes(Memory<byte> data)
        {
            var span = data.Span;
            int index = 0;

            SourcePort = span.Slice(index, 2).ToUInt16();
            index += 2;

            TargetPort = span.Slice(index, 2).ToUInt16();
            index += 2;

            TargetIp = span.Slice(index + 2, span[index]).GetUTF16String(span[index + 1]);
            index += 1 + 1 + span[index];

        }

    }
    public sealed class UdpForwardRegisterResult
    {
        public UdpForwardRegisterResultCodes Code { get; set; } = UdpForwardRegisterResultCodes.OK;
        public ulong ID { get; set; }
        public string Msg { get; set; } = string.Empty;
        public byte[] ToBytes()
        {
            var msgBytes = Msg.GetUTF16Bytes();
            var bytes = new byte[
                1
                + 8
                + 1 + 1 + msgBytes.Length
            ];
            var memory = bytes.AsMemory();
            var span = bytes.AsSpan();

            int index = 0;

            bytes[index] = (byte)Code;
            index += 1;

            ID.ToBytes(memory.Slice(index));
            index += 8;

            bytes[index] = (byte)msgBytes.Length;
            index += 1;
            bytes[index] = (byte)Msg.Length;
            index += 1;
            msgBytes.CopyTo(span.Slice(index));

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

            Msg = span.Slice(index + 2, span[index]).GetUTF16String(span[index + 1]);
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
