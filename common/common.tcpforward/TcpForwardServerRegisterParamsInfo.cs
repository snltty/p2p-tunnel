using common.libs.extends;
using System;
using System.ComponentModel;

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
            var sipBytes = SourceIp.GetUTF16Bytes();
            var tipBytes = TargetIp.GetUTF16Bytes();
            var tnameBytes = TargetName.GetUTF16Bytes();

            byte[] bytes = new byte[
                1  //AliveType
                + 2  //SourcePort
                + 2  //TargetPort
                + 1 + 1 + sipBytes.Length  //SourceIp
                + 1 + 1 + tipBytes.Length  //TargetIp
                + 1 + 1 + tnameBytes.Length //TargetName
             ];
            var memory = bytes.AsMemory();
            var span = bytes.AsSpan();

            int index = 0;

            bytes[index] = (byte)AliveType;
            index += 1;

            SourcePort.ToBytes(memory.Slice(index));
            index += 2;

            TargetPort.ToBytes(memory.Slice(index));
            index += 2;

            bytes[index] = (byte)sipBytes.Length;
            index += 1;
            bytes[index] = (byte)SourceIp.Length;
            index += 1;
            sipBytes.CopyTo(span.Slice(index));
            index += sipBytes.Length;

            bytes[index] = (byte)tipBytes.Length;
            index += 1;
            bytes[index] = (byte)TargetIp.Length;
            index += 1;
            tipBytes.CopyTo(span.Slice(index));
            index += tipBytes.Length;

            bytes[index] = (byte)tnameBytes.Length;
            index += 1;
            bytes[index] = (byte)TargetName.Length;
            index += 1;
            tnameBytes.CopyTo(span.Slice(index));
            index += tnameBytes.Length;
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

            SourceIp = span.Slice(index + 2, span[index]).GetUTF16String(span[index + 1]);
            index += 1 + 1 + span[index];

            TargetIp = span.Slice(index + 2, span[index]).GetUTF16String(span[index + 1]);
            index += 1 + 1 + span[index];

            TargetName = span.Slice(index + 2, span[index]).GetUTF16String(span[index + 1]);
            index += 1 + 1 + span[index];
        }

    }
    public sealed class TcpForwardUnRegisterParamsInfo
    {
        public TcpForwardUnRegisterParamsInfo() { }
        public string SourceIp { get; set; }
        public ushort SourcePort { get; set; }
        public TcpForwardAliveTypes AliveType { get; set; }
        public byte[] ToBytes()
        {
            var ipBytes = SourceIp.GetUTF16Bytes();
            byte[] bytes = new byte[1 +2+ 1 + ipBytes.Length];

            int index = 0;

            bytes[index] = (byte)AliveType;
            index += 1;

            SourcePort.ToBytes(bytes.AsMemory(index));
            index += 2;

            bytes[index] = (byte)SourceIp.Length;
            index += 1;
            ipBytes.CopyTo(bytes.AsSpan(index));
            index += ipBytes.Length;

            return bytes;
        }
        public void DeBytes(Memory<byte> data)
        {
            var span = data.Span;
            int index = 0;

            AliveType = (TcpForwardAliveTypes)span[index];
            index += 1;

            SourcePort = span.Slice(index, 2).ToUInt16();
            index += 2;

            SourceIp = span.Slice(index + 1).GetUTF16String(span[index]);
        }
    }
    public sealed class TcpForwardRegisterResult
    {
        public TcpForwardRegisterResultCodes Code { get; set; }
        public ulong ID { get; set; }
        public string Msg { get; set; } = string.Empty;
        public byte[] ToBytes()
        {
            var msgBytes = Msg.GetUTF16Bytes();
            var bytes = new byte[
                1
                + 8
                + 1 + 1 + msgBytes.Length];

            int index = 0;

            bytes[index] = (byte)Code;
            index += 1;

            ID.ToBytes(bytes.AsMemory(index));
            index += 8;

            bytes[index] = (byte)msgBytes.Length;
            index += 1;
            bytes[index] = (byte)Msg.Length;
            index += 1;
            msgBytes.CopyTo(bytes.AsSpan(index));

            return bytes;
        }
        public void DeBytes(ReadOnlyMemory<byte> data)
        {
            var span = data.Span;
            int index = 0;

            Code = (TcpForwardRegisterResultCodes)span[index];
            index += 1;

            ID = span.Slice(index, 8).ToUInt64();
            index += 8;

            Msg = span.Slice(index + 2, span[index]).GetUTF16String(span[index + 1]);
        }
    }

    [Flags]
    public enum TcpForwardRegisterResultCodes : byte
    {
        [Description("成功")]
        OK = 0,
        [Description("插件未开启")]
        DISABLED = 1,
        [Description("已存在")]
        EXISTS = 2,
        [Description("端口超出范围")]
        OUT_RANGE = 4,
        [Description("未知")]
        UNKNOW = 8,
    }
}
