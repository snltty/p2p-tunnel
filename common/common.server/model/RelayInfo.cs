using common.libs.extends;
using System;
using System.Reflection;

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


    [Flags, MessengerIdEnum]
    public enum RelayMessengerIds : ushort
    {
        Min = 501,
        Notify = 501,
        Verify = 502,
        Delay = 503,
        AskConnects = 504,
        Connects = 505,
        Routes = 506,
        Max = 600,
    }


    public class ConnectsInfo
    {
        public ulong Id { get; set; }
        public ulong ToId { get; set; }
        public ConnectInfo[] Connects { get; set; }

        public static ulong ReadToId(Memory<byte> memory)
        {
            return memory.Span.ToUInt64();
        }

        public byte[] ToBytes()
        {
            byte[] res = new byte[8 + 8 + 9 * Connects.Length];
            Span<byte> span = res.AsSpan();

            int index = 0;

            ToId.ToBytes().CopyTo(span);
            index += 8;

            Id.ToBytes().CopyTo(span);
            index += 8;



            for (int i = 0; i < Connects.Length; i++)
            {
                ConnectInfo item = Connects[i];

                item.Id.ToBytes().CopyTo(span.Slice(index));
                index += 8;
                span[index] = (byte)(((item.Tcp ? 1 : 0) << 1) | (item.Udp ? 1 : 0));
                index += 1;
            }
            return res;
        }

        public void DeBytes(Memory<byte> memory)
        {
            var span = memory.Span;

            int index = 0;

            ToId = span.ToUInt64();
            index += 8;

            Id = span.ToUInt64();
            index += 8;

            int len = (span.Length - 8) / 9;
            Connects = new ConnectInfo[len];
            for (int i = 0; i < len; i++)
            {
                Connects[i] = new ConnectInfo
                {
                    Id = span.Slice(index).ToUInt64(),
                    Tcp = (span[index + 1] >> 1) == 1,
                    Udp = (span[index + 1] & 0x1) == 1

                };
                index += 9;
            };
        }
    }

    public class ConnectInfo
    {
        public ulong Id { get; set; }
        public bool Tcp { get; set; }
        public bool Udp { get; set; }
    }

    public class RoutesInfo
    {
        public ulong ToId { get; set; }
        public RouteInfo To { get; set; }
        public RouteInfo Back { get; set; }

        public static ulong ReadToId(Memory<byte> memory)
        {
            return memory.Span.ToUInt64();
        }

        public byte[] ToBytes()
        {
            byte[] res = new byte[8 + 2 * 24];
            Span<byte> span = res.AsSpan();

            ToId.ToBytes().CopyTo(span);

            To.FromId.ToBytes().CopyTo(span.Slice(8));
            To.ToId.ToBytes().CopyTo(span.Slice(16));
            To.TargetId.ToBytes().CopyTo(span.Slice(24));

            Back.FromId.ToBytes().CopyTo(span.Slice(32));
            Back.ToId.ToBytes().CopyTo(span.Slice(40));
            Back.TargetId.ToBytes().CopyTo(span.Slice(48));

            return res;
        }

        public void DeBytes(Memory<byte> memory)
        {
            var span = memory.Span;

            ToId = span.ToUInt64();

            To = new RouteInfo
            {
                FromId = span.Slice(8).ToUInt64(),
                ToId = span.Slice(16).ToUInt64(),
                TargetId = span.Slice(24).ToUInt64(),
            };

            Back = new RouteInfo
            {
                FromId = span.Slice(32).ToUInt64(),
                ToId = span.Slice(40).ToUInt64(),
                TargetId = span.Slice(48).ToUInt64(),
            };
        }
    }
    public class RouteInfo
    {
        /// <summary>
        /// 哪里来
        /// </summary>
        public ulong FromId { get; set; }
        /// <summary>
        /// 到哪里
        /// </summary>
        public ulong ToId { get; set; }
        /// <summary>
        /// 选择谁
        /// </summary>
        public ulong TargetId { get; set; }
    }
}
