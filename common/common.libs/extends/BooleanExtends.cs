using System;

namespace common.libs.extends
{
    /// <summary>
    /// 
    /// </summary>
    public static class BooleanExtends
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static byte[] ToBytes(this bool val)
        {
            return BitConverter.GetBytes(val);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static bool GetBool(this byte[] bytes)
        {
            return BitConverter.ToBoolean(bytes);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static bool GetBool(this Span<byte> bytes)
        {
            return BitConverter.ToBoolean(bytes);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static bool GetBool(this ReadOnlySpan<byte> bytes)
        {
            return BitConverter.ToBoolean(bytes);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static bool GetBool(this Memory<byte> bytes)
        {
            return BitConverter.ToBoolean(bytes.Span);
        }
    }
}
