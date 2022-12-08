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
        /// <param name="value"></param>
        /// <param name="memory"></param>
        public static unsafe void ToBytes(this double value, Memory<byte> memory)
        {
            ref double v = ref value;
            fixed (void* p = &v)
            {
                new Span<byte>(p, 8).CopyTo(memory.Span);
            }
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
        /// <param name="value"></param>
        /// <param name="memory"></param>
        public static unsafe void ToBytes(this long value, Memory<byte> memory)
        {
            ref long v = ref value;
            fixed (void* p = &v)
            {
                new Span<byte>(p, 8).CopyTo(memory.Span);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="memory"></param>
        public static unsafe void ToBytes(this long[] value, Memory<byte> memory)
        {
            fixed (void* p = &value[0])
            {
                new Span<byte>(p, value.Length * sizeof(long)).CopyTo(memory.Span);
            }
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
        /// <param name="value"></param>
        /// <param name="memory"></param>
        public static unsafe void ToBytes(this ulong value, Memory<byte> memory)
        {
            ref ulong v = ref value;
            fixed (void* p = &v)
            {
                new Span<byte>(p, sizeof(ulong)).CopyTo(memory.Span);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="memory"></param>
        public static unsafe void ToBytes(this ulong[] value, Memory<byte> memory)
        {
            fixed (void* p = &value[0])
            {
                new Span<byte>(p, value.Length * 8).CopyTo(memory.Span);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="memory"></param>
        public static unsafe void ToBytes(this Memory<ulong> value, Memory<byte> memory)
        {
            fixed (void* p = &value.Span[0])
            {
                new Span<byte>(p, value.Length * 8).CopyTo(memory.Span);
            }
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
        /// <param name="value"></param>
        /// <param name="memory"></param>
        public static unsafe void ToBytes(this int value, Memory<byte> memory)
        {
            ref int v = ref value;
            fixed (void* p = &v)
            {
                new Span<byte>(p, 4).CopyTo(memory.Span);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="memory"></param>
        public static unsafe void ToBytes(this int[] value, Memory<byte> memory)
        {
            fixed (void* p = &value[0])
            {
                new Span<byte>(p, value.Length * sizeof(int)).CopyTo(memory.Span);
            }
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
        /// <param name="value"></param>
        /// <param name="memory"></param>
        public static unsafe void ToBytes(this uint value, Memory<byte> memory)
        {
            ref uint v = ref value;
            fixed (void* p = &v)
            {
                new Span<byte>(p, 4).CopyTo(memory.Span);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="memory"></param>
        public static unsafe void ToBytes(this uint[] value, Memory<byte> memory)
        {
            fixed (void* p = &value[0])
            {
                new Span<byte>(p, value.Length * sizeof(uint)).CopyTo(memory.Span);
            }
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
        /// <param name="value"></param>
        /// <param name="memory"></param>
        public static unsafe void ToBytes(this short value, Memory<byte> memory)
        {
            ref short v = ref value;
            fixed (void* p = &v)
            {
                var span = new Span<byte>(p, 2);
                memory.Span[0] = span[0];
                memory.Span[1] = span[1];
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="memory"></param>
        public static unsafe void ToBytes(this short[] value, Memory<byte> memory)
        {
            fixed (void* p = &value[0])
            {
                new Span<byte>(p, value.Length * sizeof(short)).CopyTo(memory.Span);
            }
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
        /// <param name="value"></param>
        /// <param name="memory"></param>
        public static unsafe void ToBytes(this ushort value, Memory<byte> memory)
        {
            ref ushort v = ref value;
            fixed (void* p = &v)
            {
                var span = new Span<byte>(p, 2);
                memory.Span[0] = span[0];
                memory.Span[1] = span[1];
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="memory"></param>
        public static unsafe void ToBytes(this ushort[] value, Memory<byte> memory)
        {
            fixed (void* p = &value[0])
            {
                new Span<byte>(p, value.Length * sizeof(ushort)).CopyTo(memory.Span);
            }
        }

        #region 反序列化

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
        #endregion
    }
}
