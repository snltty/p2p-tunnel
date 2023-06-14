using System;
using System.Text;

namespace common.proxy
{
    public static class ProxyHelper
    {
        public static string MagicString = "\0S-n-l-t-t-y-S-n-l-t-t-y-T-e-s-t";
        public static byte[] MagicData { get; } = Encoding.UTF8.GetBytes(MagicString);

        public static bool GetIsMagicData(Memory<byte> data)
        {
            if (data.Length < MagicData.Length) return false;
            return data.Slice(0, MagicData.Length).Span.SequenceEqual(MagicData);
        }
    }
}
