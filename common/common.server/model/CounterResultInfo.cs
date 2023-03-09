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
        public int OnlineCount { get; set; }
        /// <summary>
        /// cpu使用率
        /// </summary>
        public double Cpu { get; set; }
        /// <summary>
        /// 内存
        /// </summary>
        public double Memory { get; set; }
        /// <summary>
        /// 运行时间
        /// </summary>
        public int RunTime { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public long SendBytes { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public long ReceiveBytes { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            var bytes = new byte[4 + 8 + 8 + 4 + 8 + 8];
            var memory = bytes.AsMemory();
            int index = 0;

            OnlineCount.ToBytes(memory.Slice(index));
            index += 4;

            Cpu.ToBytes(memory.Slice(index));
            index += 8;

            Memory.ToBytes(memory.Slice(index));
            index += 8;

            RunTime.ToBytes(memory.Slice(index));
            index += 4;

            SendBytes.ToBytes(memory.Slice(index));
            index += 8;

            ReceiveBytes.ToBytes(memory.Slice(index));
            index += 8;

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

            SendBytes = span.Slice(index, 8).ToInt64();
            index += 8;

            ReceiveBytes = span.Slice(index, 8).ToInt64();
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