using common.libs.extends;
using System;

namespace common.server.model
{
    public class CounterResultInfo
    {
        public int OnlineCount { get; set; } = 0;
        public double Cpu { get; set; } = 0;
        public double Memory { get; set; } = 0;
        public int RunTime { get; set; } = 0;
        public long TcpSendBytes { get; set; } = 0;
        public long TcpReceiveBytes { get; set; } = 0;
        public long UdpSendBytes { get; set; } = 0;
        public long UdpReceiveBytes { get; set; } = 0;

        public byte[] ToBytes()
        {
            var bytes = new byte[4 + 8 + 8 + 4 + 8 + 8 + 8 + 8];
            int index = 0;

            var onlineCountBytes = OnlineCount.ToBytes();
            Array.Copy(onlineCountBytes, 0, bytes, index, onlineCountBytes.Length);
            index += onlineCountBytes.Length;

            var cpuBytes = Cpu.ToBytes();
            Array.Copy(cpuBytes, 0, bytes, index, cpuBytes.Length);
            index += cpuBytes.Length;

            var memoryBytes = Memory.ToBytes();
            Array.Copy(memoryBytes, 0, bytes, index, memoryBytes.Length);
            index += memoryBytes.Length;

            var tuntimeBytes = RunTime.ToBytes();
            Array.Copy(tuntimeBytes, 0, bytes, index, tuntimeBytes.Length);
            index += tuntimeBytes.Length;

            var tcpsendBytes = TcpSendBytes.ToBytes();
            Array.Copy(tcpsendBytes, 0, bytes, index, tcpsendBytes.Length);
            index += tcpsendBytes.Length;

            var tcpreceiveBytes = TcpReceiveBytes.ToBytes();
            Array.Copy(tcpreceiveBytes, 0, bytes, index, tcpreceiveBytes.Length);
            index += tcpreceiveBytes.Length;

            var udpsendBytes = UdpSendBytes.ToBytes();
            Array.Copy(udpsendBytes, 0, bytes, index, udpsendBytes.Length);
            index += udpsendBytes.Length;

            var udpreceiveBytes = UdpReceiveBytes.ToBytes();
            Array.Copy(udpreceiveBytes, 0, bytes, index, udpreceiveBytes.Length);
            index += udpreceiveBytes.Length;

            return bytes;
        }
        public void DeBytes(ReadOnlyMemory<byte> data)
        {
            var span = data.Span;

            int index = 0;

            OnlineCount = span.Slice(index, 4).ToInt32();
            index += 4;

            Cpu = span.Slice(index, 8).ToDouble();
            index += 8;

            Memory = span.Slice(index, 8).ToDouble();
            index += 8;

            RunTime = span.Slice(index, 4).ToInt32();
            index += 4;

            TcpSendBytes = span.Slice(index, 8).ToInt64();
            index += 8;

            TcpReceiveBytes = span.Slice(index, 8).ToInt64();
            index += 8;

            UdpSendBytes = span.Slice(index, 8).ToInt64();
            index += 8;

            UdpReceiveBytes = span.Slice(index, 8).ToInt64();
            index += 8;
        }
    }

    [Flags, MessengerIdEnum]
    public enum CounterMessengerIds : ushort
    {
        Min = 1101,
        Info = 1101,
        Max = 1201,
    }
}