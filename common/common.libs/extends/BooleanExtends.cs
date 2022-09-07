using System;

namespace common.libs.extends
{
    public static class BooleanExtends
    {
        public static byte[] ToBytes(this bool val)
        {
            return BitConverter.GetBytes(val);
        }
        public static bool GetBool(this byte[] bytes)
        {
            return BitConverter.ToBoolean(bytes);
        }
        public static bool GetBool(this Span<byte> bytes)
        {
            return BitConverter.ToBoolean(bytes);
        }
        public static bool GetBool(this ReadOnlySpan<byte> bytes)
        {
            return BitConverter.ToBoolean(bytes);
        }
        public static bool GetBool(this Memory<byte> bytes)
        {
            return BitConverter.ToBoolean(bytes.Span);
        }
    }
}
