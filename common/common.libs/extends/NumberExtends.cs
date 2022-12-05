using System;

namespace common.libs.extends
{
    /// <summary>
    /// 
    /// </summary>
    public static class NumberExtends
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        public static byte[] ToBytes(this double num)
        {
            return BitConverter.GetBytes(num);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="startindex"></param>
        /// <returns></returns>
        public static double ToDouble(this byte[] bytes, int startindex = 0)
        {
            return BitConverter.ToDouble(bytes, startindex);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        public static double ToDouble(this Span<byte> span)
        {
            return BitConverter.ToDouble(span);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        public static double ToDouble(this ReadOnlySpan<byte> span)
        {
            return BitConverter.ToDouble(span);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        public static byte[] ToBytes(this long num)
        {
            return BitConverter.GetBytes(num);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        public static byte[] ToBytes(this ulong num)
        {
            return BitConverter.GetBytes(num);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="startindex"></param>
        /// <returns></returns>
        public static long ToInt64(this byte[] bytes, int startindex = 0)
        {
            return BitConverter.ToInt64(bytes, startindex);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="startindex"></param>
        /// <returns></returns>
        public static ulong ToUInt64(this byte[] bytes, int startindex = 0)
        {
            return BitConverter.ToUInt64(bytes, startindex);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        public static long ToInt64(this Span<byte> span)
        {
            return BitConverter.ToInt64(span);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        public static ulong ToUInt64(this Span<byte> span)
        {
            return BitConverter.ToUInt64(span);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        public static long ToInt64(this ReadOnlySpan<byte> span)
        {
            return BitConverter.ToInt64(span);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        public static ulong ToUInt64(this ReadOnlySpan<byte> span)
        {
            return BitConverter.ToUInt64(span);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        public static byte[] ToBytes(this int num)
        {
            return BitConverter.GetBytes(num);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        public static byte[] ToBytes(this uint num)
        {
            return BitConverter.GetBytes(num);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="startindex"></param>
        /// <returns></returns>
        public static int ToInt32(this byte[] bytes, int startindex = 0)
        {
            return BitConverter.ToInt32(bytes, startindex);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="startindex"></param>
        /// <returns></returns>
        public static uint ToUInt32(this byte[] bytes, int startindex = 0)
        {
            return BitConverter.ToUInt32(bytes, startindex);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        public static int ToInt32(this Span<byte> span)
        {
            return BitConverter.ToInt32(span);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        public static uint ToUInt32(this Span<byte> span)
        {
            return BitConverter.ToUInt32(span);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        public static int ToInt32(this ReadOnlySpan<byte> span)
        {
            return BitConverter.ToInt32(span);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        public static uint ToUInt32(this ReadOnlySpan<byte> span)
        {
            return BitConverter.ToUInt32(span);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        public static byte[] ToBytes(this short num)
        {
            return BitConverter.GetBytes(num);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        public static byte[] ToBytes(this ushort num)
        {
            return BitConverter.GetBytes(num);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="startindex"></param>
        /// <returns></returns>
        public static short ToInt16(this byte[] bytes, int startindex = 0)
        {
            return BitConverter.ToInt16(bytes, startindex);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="startindex"></param>
        /// <returns></returns>
        public static ushort ToUInt16(this byte[] bytes, int startindex = 0)
        {
            return BitConverter.ToUInt16(bytes, startindex);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        public static short ToInt16(this Span<byte> span)
        {
            return BitConverter.ToInt16(span);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        public static ushort ToUInt16(this Span<byte> span)
        {
            return BitConverter.ToUInt16(span);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        public static short ToInt16(this ReadOnlySpan<byte> span)
        {
            return BitConverter.ToInt16(span);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        public static ushort ToUInt16(this ReadOnlySpan<byte> span)
        {
            return BitConverter.ToUInt16(span);
        }

    }
}
