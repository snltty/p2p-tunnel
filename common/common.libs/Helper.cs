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


        public static string SeparatorString = ",";
        public static char SeparatorChar = ',';
        public static string Version = "2.1.0.0";

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
    }
}
