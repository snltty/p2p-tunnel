using common.libs;
using common.libs.extends;
using System;
using System.Text;

namespace common.tcpforward
{
    public static class HttpParseHelper
    {
        private static byte[] hostBytes = Encoding.ASCII.GetBytes("host: ");
        /// <summary>
        /// 从http报文中获取host
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        public static Span<byte> GetHost(in Span<byte> span)
        {
            int keyIndex = -1;
            for (int i = 0, len = span.Length; i < len; i++)
            {
                //找到key之前
                if (keyIndex == -1)
                {
                    if (span[i] == 10)
                    {
                        //两个换行，headers已结束
                        if (span[i + 1] == 10)
                        {
                            break;
                        }
                        //因为 headers 是从第二行开始，所以，可以在碰到每个\n时，向前获取与目标key相同长度的内容与之匹配，成功则已找到key，标识位置
                        Span<byte> keySpan = span.Slice(i + 1, hostBytes.Length);
                        keySpan[0] |= 32;
                        if (keySpan[0] == hostBytes[0] && keySpan.SequenceEqual(hostBytes))
                        {
                            keyIndex = i + 1 + hostBytes.Length;
                            i += hostBytes.Length - 1;
                        }
                    }
                }
                //找到key之后，如果碰到了\n，就说明value内容已结束，截取key的位置到当前\n位置，就是值内容
                else if (span[i] == 10)
                {
                    Span<byte> valueSpan = span.Slice(keyIndex, i - 1 - keyIndex);
                    return valueSpan;
                }
            }
            return Helper.EmptyArray;
        }
        /// <summary>
        /// 构造一条简单的http消息
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static byte[] BuildMessage(string msg)
        {
            var bytes = Encoding.UTF8.GetBytes(msg);

            StringBuilder sb = new StringBuilder();
            sb.Append("HTTP/1.1 200 OK\r\n");
            sb.Append("Content-Type: text/html;charset=utf-8\r\n");
            sb.Append($"Content-Length: {bytes.Length}\r\n");
            sb.Append("Access-Control-Allow-Credentials: true\r\n");
            sb.Append("Access-Control-Allow-Headers: *\r\n");
            sb.Append("Access-Control-Allow-Methods: *\r\n");
            sb.Append("Access-Control-Allow-Origin: *\r\n");
            sb.Append("\r\n");

            var headerBytes = sb.ToString().ToBytes();
            var res = new byte[headerBytes.Length + bytes.Length];

            Array.Copy(headerBytes, 0, res, 0, headerBytes.Length);
            Array.Copy(bytes, 0, res, headerBytes.Length, bytes.Length);

            return res;
        }
    }
}
