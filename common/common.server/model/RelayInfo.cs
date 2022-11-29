using common.libs;
using common.libs.extends;
using System;

namespace common.server.model
{
    public class RelayInfo
    {
        public IConnection Connection { get; set; }
        public ulong[] RelayIds { get; set; } = Helper.EmptyUlongArray;
        public byte[] ToBytes()
        {
            var bytes = new byte[RelayIds.Length * MessageRequestWrap.RelayIdSize];

            for (int i = 0; i < RelayIds.Length; i++)
            {
                RelayIds[i].ToBytes().AsSpan().CopyTo(bytes.AsSpan(i * MessageRequestWrap.RelayIdSize));
            }
            return bytes;
        }

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

        public static void ClearToId(Memory<byte> data)
        {
            0.ToBytes().AsSpan().CopyTo(data.Span.Slice(8));
        }
    }


    [Flags, MessengerIdEnum]
    public enum RelayMessengerIds : ushort
    {
        Min = 500,
        Relay = 501,
        Delay = 502,
        AskConnects = 503,
        Connects = 504,
        Max = 599,
    }


    public class ConnectsInfo
    {
        public ulong Id { get; set; }
        public ulong ToId { get; set; }
        public ulong[] Connects { get; set; }

        public static ulong ReadToId(Memory<byte> memory)
        {
            return memory.Span.ToUInt64();
        }

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
