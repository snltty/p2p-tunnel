using System;
using System.Diagnostics;
using System.Net;

namespace common.libs
{
    /// <summary>
    /// 
    /// </summary>
    public static class Helper
    {
        /// <summary>
        /// 
        /// </summary>
        public static byte[] EmptyArray = Array.Empty<byte>();
        /// <summary>
        /// 
        /// </summary>
        public static byte[] TrueArray = new byte[] { 1 };
        /// <summary>
        /// 
        /// </summary>
        public static byte[] FalseArray = new byte[] { 0 };
        /// <summary>
        /// 
        /// </summary>
        public static byte[] AnyIpArray = IPAddress.Any.GetAddressBytes();
        /// <summary>
        /// 
        /// </summary>
        public static byte[] AnyIpv6Array = IPAddress.IPv6Any.GetAddressBytes();
        /// <summary>
        /// 
        /// </summary>
        public static byte[] AnyPoryArray = new byte[] { 0, 0 };


        /// <summary>
        /// 
        /// </summary>
        public static string SeparatorString = ",";
        /// <summary>
        /// 
        /// </summary>
        public static char SeparatorChar = ',';
        /// <summary>
        /// 
        /// </summary>
        public static byte[] Version = new byte[4] { 2, 0, 0, 0 };

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
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
