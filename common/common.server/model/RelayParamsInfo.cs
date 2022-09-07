using common.libs;
using common.libs.extends;
using System;

namespace common.server.model
{
    /// <summary>
    /// 中继数据包
    /// </summary>
    public class RelayParamsInfo
    {
        public RelayParamsInfo() { }

        /// <summary>
        /// 给谁
        /// </summary>
        public ulong ToId { get; set; } = 0;

        /// <summary>
        /// 给目标端哪个路径
        /// </summary>
        public Memory<byte> Path { get; set; }

        /// <summary>
        /// 数据
        /// </summary>
        public Memory<byte> Data { get; set; } = Helper.EmptyArray;

        public byte[] ToBytes()
        {
            var idBytes = ToId.ToBytes();
            var bytes = new byte[idBytes.Length + 1 + Path.Length + Data.Length];

            int index = 0;

            Array.Copy(idBytes, 0, bytes, index, idBytes.Length);
            index += idBytes.Length;

            bytes[index] = (byte)Path.Length;
            index += 1;
            Path.CopyTo(bytes.AsMemory(index, Path.Length));
            index += Path.Length;

            Data.CopyTo(bytes.AsMemory(index, Data.Length));

            return bytes;
        }

        public void DeBytes(Memory<byte> data)
        {
            var span = data.Span;
            int index = 0;

            ToId = span.Slice(index, 8).ToUInt64();
            index += 8;

            int length = span[index];
            index += 1;
            Path = data.Slice(index, length);
            index += length;

            Data = data.Slice(index);
        }
    }
}
