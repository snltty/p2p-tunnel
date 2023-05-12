using System;
using System.Diagnostics;
using System.Net;

namespace common.libs
{
    public static class Helper
    {
        public static byte[] EmptyArray = Array.Empty<byte>();
        public static byte[] TrueArray = new byte[] { 1 };
        public static byte[] FalseArray = new byte[] { 0 };
        public static byte[] AnyIpArray = IPAddress.Any.GetAddressBytes();
        public static byte[] AnyIpv6Array = IPAddress.IPv6Any.GetAddressBytes();
        public static byte[] AnyPoryArray = new byte[] { 0, 0 };

        /// <summary>
        /// 空格
        /// </summary>
        public static char SeparatorCharSpace = ' ';
        /// <summary>
        /// 逗号
        /// </summary>
        public static char SeparatorCharComma = ',';
        /// <summary>
        /// 斜杠
        /// </summary>
        public static char SeparatorCharSlash = '/';
        public static string Version = "2.1.0.0-beta";

        public static string GetStackTraceModelName()
        {
            string result = "";
            var stacktrace = new StackTrace();
            for (var i = 0; i < stacktrace.FrameCount; i++)
            {
                var method = stacktrace.GetFrame(i).GetMethod();
                result += (stacktrace.GetFrame(i).GetFileName() + "->" + method.Name + "\n");
            }
            return result;
        }

        public static ushort[] Range(ushort start, ushort end)
        {
            ushort[] result = new ushort[(end - start) + 1];
            for (ushort p = start, i = 0; p <= end; p++, i++)
            {
                result[i] = p;
            }
            return result;
        }
    }
}
