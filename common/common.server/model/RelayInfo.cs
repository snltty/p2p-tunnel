using common.libs.extends;
using System;

namespace common.server.model
{
    public class RelayInfo
    {
        public ulong FromId { get; set; } = 0;
        public ulong ToId { get; set; } = 0;
        public byte[] ToBytes()
        {
            var bytes = new byte[8 + 8];

            FromId.ToBytes().AsSpan().CopyTo(bytes.AsSpan(0));
            ToId.ToBytes().AsSpan().CopyTo(bytes.AsSpan(8));

            return bytes;
        }

        public void DeBytes(ReadOnlyMemory<byte> data)
        {
            var span = data.Span;
            FromId = span.ToUInt64();
            ToId = span.Slice(8).ToUInt64();
        }

        public static void ClearToId(Memory<byte> data)
        {
            0.ToBytes().AsSpan().CopyTo(data.Span.Slice(8));
        }
    }
}
