using common.libs.extends;
using System;

namespace common.server.model
{
    /// <summary>
    /// 服务器信息
    /// </summary>
    public sealed class CounterResultInfo
    {
        /// <summary>
        /// 在线人数
        /// </summary>
        public int OnlineCount { get; set; } = 0;
        /// <summary>
        /// cpu使用率
        /// </summary>
        public double Cpu { get; set; } = 0;
        /// <summary>
        /// 内存
        /// </summary>
        public double Memory { get; set; } = 0;
        /// <summary>
        /// 运行时间
        /// </summary>
        public int RunTime { get; set; } = 0;
        /// <summary>
        /// 
        /// </summary>
        public long TcpSendBytes { get; set; } = 0;
        /// <summary>
        /// 
        /// </summary>
        public long TcpReceiveBytes { get; set; } = 0;
        /// <summary>
        /// 
        /// </summary>
        public long UdpSendBytes { get; set; } = 0;
        /// <summary>
        /// 
        /// </summary>
        public long UdpReceiveBytes { get; set; } = 0;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
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

    /// <summary>
    /// 服务器信息相关消息id
    /// </summary>
    [Flags, MessengerIdEnum]
    public enum CounterMessengerIds : ushort
    {
        /// <summary>
        /// 
        /// </summary>
        Min = 1100,
        /// <summary>
        /// 获取信息
        /// </summary>
        Info = 1101,
        /// <summary>
        /// 
        /// </summary>
        Max = 1199,
    }
}