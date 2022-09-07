using common.libs;
using common.libs.extends;
using System;
using System.Linq;
using System.Text;

namespace common.tcpforward
{
    public static class HttpConnectMethodHelper
    {
        /// <summary>
        /// http connect 成功报文
        /// </summary>
        /// <returns></returns>
        public static byte[] ConnectSuccessMessage()
        {
            return "HTTP/1.1 200 Connection Established\r\n\r\n".ToBytes();
        }
        /// <summary>
        /// http connect 失败报文
        /// </summary>
        /// <returns></returns>
        public static byte[] ConnectErrorMessage()
        {
            return "HTTP/1.1 407 Unauthorized\r\n\r\n".ToBytes();
        }

        private static Memory<byte> connectMethodValue = Encoding.ASCII.GetBytes("CONNECT");
        /// <summary>
        /// 判断http报文是否是connect方法
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        public static bool IsConnectMethod(in Span<byte> span)
        {
            return span.Length > connectMethodValue.Length && span.Slice(0, connectMethodValue.Length).SequenceEqual(connectMethodValue.Span);
        }
        /// <summary>
        /// 获取 http connect方法中的host
        /// </summary>
        /// <param name="memory"></param>
        /// <returns></returns>
        public static Memory<byte> GetHost(in Memory<byte> memory)
        {
            var span = memory.Span;
            int start = connectMethodValue.Length + 1;
            int hostEndindex = -1, portEndIndex = -1;
            for (int i = start, len = span.Length; i < len; i++)
            {
                if (span[i] == 58)
                {
                    hostEndindex = i + 1;
                }
                if (span[i] == 32)
                {
                    portEndIndex = i + 1;
                    break;
                }
                if (span[i] == 10)
                {
                    break;
                }
            }
            if (hostEndindex >= 0 && portEndIndex >= 0)
            {
                var host = memory.Slice(start, hostEndindex - start - 1);
                var port = Array2Int(memory.Slice(hostEndindex, portEndIndex - hostEndindex - 1));

                return NetworkHelper.EndpointToArray(host, port);
            }
            return Array.Empty<byte>();
        }
        private static Memory<byte> Array2Int(in Memory<byte> memory)
        {
            var span = memory.Span;
            int result = 0;
            for (int i = 0, len = span.Length; i < len; i++)
            {
                result += (span[i] - 48) * (int)Math.Pow(10, len - i - 1);
            }
            return result.ToBytes();
        }
    }
}
