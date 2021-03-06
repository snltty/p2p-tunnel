using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace common.libs.extends
{
    public static class SpanExtends
    {
        public static ulong ToUInt64(this Span<byte> span)
        {
            return BitConverter.ToUInt64(span);
        }
        public static long ToInt64(this Span<byte> span)
        {
            return BitConverter.ToInt64(span);
        }
        public static ulong ToUInt64(this ReadOnlySpan<byte> span)
        {
            return BitConverter.ToUInt64(span);
        }
        public static long ToInt64(this ReadOnlySpan<byte> span)
        {
            return BitConverter.ToInt64(span);
        }
        public static long ToInt64(this Memory<byte> memory)
        {
            return BitConverter.ToInt64(memory.Span);
        }

        public static int ToInt32(this Span<byte> span)
        {
            return BitConverter.ToInt32(span);
        }
        public static int ToInt32(this ReadOnlySpan<byte> span)
        {
            return BitConverter.ToInt32(span);
        }


        public static short ToInt16(this Span<byte> span)
        {
            return BitConverter.ToInt16(span);
        }
        public static short ToInt16(this ReadOnlySpan<byte> span)
        {
            return BitConverter.ToInt16(span);
        }

        public static string GetString(this Span<byte> span)
        {
            return Encoding.UTF8.GetString(span);
        }
        public static string GetString(this ReadOnlySpan<byte> span)
        {
            return Encoding.UTF8.GetString(span);
        }

    }
}
