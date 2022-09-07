using System;

namespace common.libs.extends
{
    public static class NumberExtends
    {
        public static byte[] ToBytes(this double num)
        {
            return BitConverter.GetBytes(num);
        }
        public static double ToDouble(this byte[] bytes, int startindex = 0)
        {
            return BitConverter.ToDouble(bytes, startindex);
        }
        public static double ToDouble(this Span<byte> span)
        {
            return BitConverter.ToDouble(span);
        }
        public static double ToDouble(this ReadOnlySpan<byte> span)
        {
            return BitConverter.ToDouble(span);
        }


        public static byte[] ToBytes(this long num)
        {
            return BitConverter.GetBytes(num);
        }
        public static byte[] ToBytes(this ulong num)
        {
            return BitConverter.GetBytes(num);
        }
        public static long ToInt64(this byte[] bytes, int startindex = 0)
        {
            return BitConverter.ToInt64(bytes, startindex);
        }
        public static ulong ToUInt64(this byte[] bytes, int startindex = 0)
        {
            return BitConverter.ToUInt64(bytes, startindex);
        }
        public static long ToInt64(this Span<byte> span)
        {
            return BitConverter.ToInt64(span);
        }
        public static ulong ToUInt64(this Span<byte> span)
        {
            return BitConverter.ToUInt64(span);
        }
        public static long ToInt64(this ReadOnlySpan<byte> span)
        {
            return BitConverter.ToInt64(span);
        }
        public static ulong ToUInt64(this ReadOnlySpan<byte> span)
        {
            return BitConverter.ToUInt64(span);
        }

        public static byte[] ToBytes(this int num)
        {
            return BitConverter.GetBytes(num);
        }
        public static byte[] ToBytes(this uint num)
        {
            return BitConverter.GetBytes(num);
        }
        public static int ToInt32(this byte[] bytes, int startindex = 0)
        {
            return BitConverter.ToInt32(bytes, startindex);
        }
        public static uint ToUInt32(this byte[] bytes, int startindex = 0)
        {
            return BitConverter.ToUInt32(bytes, startindex);
        }
        public static int ToInt32(this Span<byte> span)
        {
            return BitConverter.ToInt32(span);
        }
        public static uint ToUInt32(this Span<byte> span)
        {
            return BitConverter.ToUInt32(span);
        }
        public static int ToInt32(this ReadOnlySpan<byte> span)
        {
            return BitConverter.ToInt32(span);
        }
        public static uint ToUInt32(this ReadOnlySpan<byte> span)
        {
            return BitConverter.ToUInt32(span);
        }

        public static byte[] ToBytes(this short num)
        {
            return BitConverter.GetBytes(num);
        }
        public static byte[] ToBytes(this ushort num)
        {
            return BitConverter.GetBytes(num);
        }
        public static short ToInt16(this byte[] bytes, int startindex = 0)
        {
            return BitConverter.ToInt16(bytes, startindex);
        }
        public static ushort ToUInt16(this byte[] bytes, int startindex = 0)
        {
            return BitConverter.ToUInt16(bytes, startindex);
        }
        public static short ToInt16(this Span<byte> span)
        {
            return BitConverter.ToInt16(span);
        }
        public static ushort ToUInt16(this Span<byte> span)
        {
            return BitConverter.ToUInt16(span);
        }
        public static short ToInt16(this ReadOnlySpan<byte> span)
        {
            return BitConverter.ToInt16(span);
        }
        public static ushort ToUInt16(this ReadOnlySpan<byte> span)
        {
            return BitConverter.ToUInt16(span);
        }

    }
}
