using common.libs;
using common.libs.extends;
using System;

namespace common.server.model
{
    /// <summary>
    /// 中继
    /// </summary>
    public sealed class RelayInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public IConnection Connection { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public ulong[] RelayIds { get; set; } = Helper.EmptyUlongArray;
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            var bytes = new byte[RelayIds.Length * MessageRequestWrap.RelayIdSize];

            for (int i = 0; i < RelayIds.Length; i++)
            {
                RelayIds[i].ToBytes().AsSpan().CopyTo(bytes.AsSpan(i * MessageRequestWrap.RelayIdSize));
            }
            return bytes;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public void DeBytes(ReadOnlyMemory<byte> data)
        {
            var span = data.Span;
            int length = data.Length / MessageRequestWrap.RelayIdSize;
            RelayIds = new ulong[length];
            for (int i = length - 1; i >= 0; i--)
            {
                RelayIds[length - 1 - i] = span.Slice(i * MessageRequestWrap.RelayIdSize).ToUInt64();
            }
        }
    }

    /// <summary>
    /// 中继相关消息id
    /// </summary>
    [Flags, MessengerIdEnum]
    public enum RelayMessengerIds : ushort
    {
        /// <summary>
        /// 
        /// </summary>
        Min = 500,
        /// <summary>
        /// 中继
        /// </summary>
        Relay = 501,
        /// <summary>
        /// 延迟
        /// </summary>
        Delay = 502,
        /// <summary>
        /// 请求连接信息
        /// </summary>
        AskConnects = 503,
        /// <summary>
        /// 回复连接信息
        /// </summary>
        Connects = 504,
        /// <summary>
        /// 
        /// </summary>
        Max = 599,
    }

    /// <summary>
    /// 连接信息
    /// </summary>
    public sealed class ConnectsInfo
    {
        /// <summary>
        /// 我
        /// </summary>
        public ulong Id { get; set; }
        /// <summary>
        /// 给谁
        /// </summary>
        public ulong ToId { get; set; }
        /// <summary>
        /// 我连了谁
        /// </summary>
        public ulong[] Connects { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="memory"></param>
        /// <returns></returns>
        public static ulong ReadToId(Memory<byte> memory)
        {
            return memory.Span.ToUInt64();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            byte[] res = new byte[8 + 8 + 8 * Connects.Length];
            Span<byte> span = res.AsSpan();

            int index = 0;

            ToId.ToBytes().CopyTo(span);
            index += 8;

            Id.ToBytes().CopyTo(span.Slice(index));
            index += 8;

            for (int i = 0; i < Connects.Length; i++)
            {
                Connects[i].ToBytes().CopyTo(span.Slice(index));
                index += 8;
            }
            return res;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="memory"></param>
        public void DeBytes(Memory<byte> memory)
        {
            var span = memory.Span;

            int index = 0;

            ToId = span.ToUInt64();
            index += 8;

            Id = span.Slice(index).ToUInt64();
            index += 8;

            int len = (span.Length - 8) / 9;
            Connects = new ulong[len];
            for (int i = 0; i < len; i++)
            {
                Connects[i] = span.Slice(index).ToUInt64();
                index += 8;
            };
        }
    }
}
