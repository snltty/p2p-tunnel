using common.libs;
using common.libs.extends;
using common.server;
using System;
using System.Buffers;
using System.Net;

namespace common.udpforward
{
    /// <summary>
    /// udp转发数据
    /// </summary>
    public sealed class UdpForwardInfo
    {
        public UdpForwardInfo() { }
        public ushort SourcePort { get; set; }

        /// <summary>
        /// 来源地址，是谁发的数据，目标端回复的时候知道回复给谁
        /// </summary>
        public IPEndPoint SourceEndpoint { get; set; }
        /// <summary>
        /// 目标地址，发给谁
        /// </summary>
        public Memory<byte> TargetEndpoint { get; set; }
        /// <summary>
        /// 发送的数据
        /// </summary>
        public Memory<byte> Buffer { get; set; }

        public IConnection Connection { get; set; }

        public byte[] ToBytes(out int length)
        {
            int ipLength = SourceEndpoint.Address.Length();

            length = 1 + ipLength + 2 + // endpoint
                 1 + TargetEndpoint.Length + // endpoint
                Buffer.Length;
            var bytes = ArrayPool<byte>.Shared.Rent(length);
            var memory = bytes.AsMemory();

            int index = 0;

            bytes[index] = (byte)ipLength;
            index++;

            SourceEndpoint.Address.TryWriteBytes(memory.Slice(index).Span,out _);
            index += ipLength;

            SourceEndpoint.Port.ToBytes(memory.Slice(index));
            index += 2;

            bytes[index] = (byte)TargetEndpoint.Length;
            index++;
            TargetEndpoint.CopyTo(memory.Slice(index));
            index += TargetEndpoint.Length;

            Buffer.CopyTo(memory.Slice(index));
            index += Buffer.Length;
            return bytes;
        }

        public void DeBytes(in Memory<byte> memory)
        {
            var span = memory.Span;
            int index = 0;

            byte endpointLength = span[index];
            index++;

            IPAddress ip = new IPAddress(span.Slice(index, endpointLength));
            index += endpointLength;

            int port = span.Slice(index, 2).ToUInt16();
            index += 2;
            SourceEndpoint = new IPEndPoint(ip, port);

            endpointLength = span[index];
            index++;
            TargetEndpoint = memory.Slice(index, endpointLength);
            index += endpointLength;

            Buffer = memory.Slice(index);
        }

        public void Return(byte[] data)
        {
            ArrayPool<byte>.Shared.Return(data);
        }
    }


    /// <summary>
    /// udp转发相关消息id
    /// </summary>
    [Flags, MessengerIdEnum]
    public enum UdpForwardMessengerIds : ushort
    {
        /// <summary>
        /// 
        /// </summary>
        Min = 700,
        /// <summary>
        /// 请求
        /// </summary>
        Request = 702,
        /// <summary>
        /// 回执
        /// </summary>
        Response = 703,
        /// <summary>
        /// 端口
        /// </summary>
        Ports = 704,
        /// <summary>
        /// 注册
        /// </summary>
        SignIn = 705,
        /// <summary>
        /// 退出
        /// </summary>
        SignOut = 706,
        /// <summary>
        /// 获取配置
        /// </summary>
        GetSetting = 707,
        /// <summary>
        /// 配置
        /// </summary>
        Setting = 708,
        /// <summary>
        /// 
        /// </summary>
        Max = 799,
    }
}
