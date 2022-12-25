using System.Diagnostics;
using System.Net;

namespace socks5
{
    public static class Helper
    {
        public static byte[] EmptyArray = Array.Empty<byte>();
        public static byte[] TrueArray = new byte[] { 1 };
        public static byte[] FalseArray = new byte[] { 0 };
        public static byte[] AnyIpArray = IPAddress.Any.GetAddressBytes();


        public static string SeparatorString = ",";
        public static char SeparatorChar = ',';
        public static int Version = 1;
        public static string GetStackTraceModelName()
        {
            string result = "";
            var stacktrace = new StackTrace();
            for (var i = 0; i < stacktrace.FrameCount; i++)
            {
                var method = stacktrace.GetFrame(i).GetMethod();
                result += (stacktrace.GetFrame(i).GetFileName() + "->" + method.Name +"\n");
            }
            return result;
        }
    }
}
