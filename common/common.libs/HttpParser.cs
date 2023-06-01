using common.libs;
using common.libs.extends;
using System;
using System.Text;

namespace common.libs
{
    public static class HttpParser
    {
        private static byte[] hostBytes = Encoding.ASCII.GetBytes("host: ");
        private static byte[] httpByte = Encoding.UTF8.GetBytes("HTTP");
        private static byte[][] headers = new byte[][] {
            Encoding.UTF8.GetBytes("GET /"),
            Encoding.UTF8.GetBytes("POST /"),
            Encoding.UTF8.GetBytes("OPTIONS /"),
            Encoding.UTF8.GetBytes("PUT /"),
            Encoding.UTF8.GetBytes("DELETE /"),
            Encoding.UTF8.GetBytes("PATCH /"),
            Encoding.UTF8.GetBytes("HEAD /"),
        };

        /// <summary>
        /// 从http报文中获取host
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        public static Memory<byte> GetHost(in Memory<byte> memory, ref int portStart)
        {
            int keyIndex = -1;
            var span = memory.Span;
            for (int i = 0, len = span.Length; i < len; i++)
            {
                //找到key之前
                if (keyIndex == -1)
                {
                    if (span[i] == 10)
                    {
                        //索引超出 或者 两个换行，headers已结束
                        if (i + 1 + hostBytes.Length >= span.Length || i + 1 >= span.Length || span[i + 1] == 10)
                        {
                            break;
                        }
                        //因为 headers 是从第二行开始，所以，可以在碰到每个\n时，向后获取与目标key相同长度的内容与之匹配，成功则已找到key，标识位置
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
                    return memory.Slice(keyIndex, i - 1 - keyIndex);
                }
                else if (span[i] == 58)
                {
                    portStart = i - keyIndex;
                }
            }
            return Array.Empty<byte>();
        }

        /// <summary>
        /// 构造一条简单的http消息
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static byte[] BuildMessage(string msg)
        {
            var bytes = msg.ToBytes();

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


        public static int IsHttp(Memory<byte> data)
        {
            if (data.Length < 9) return 0;

            Span<byte> span = data.Span;
            int firstSpace = 0;

            for (int i = 0; i < headers.Length; i++)
            {
                //判断是否属于某个HTTP METHOD
                if (span.Slice(0, headers[i].Length).SequenceEqual(headers[i]))
                {
                    //查找换行，并截取判断是否存在HTTP关键字
                    for (int index = headers[i].Length; index < span.Length; index++)
                    {
                        //碰到空格，记录一下，用于截取HTTP关键字
                        if (span[index] == 32)
                        {
                            firstSpace = index+1;
                        }
                        else if (span[index] == 10)
                        {
                            //找到换行，并且截取到了HTTP关键字，表示这是一个http协议，否则是误判了
                            if (firstSpace > 0 && span.Slice(firstSpace, httpByte.Length).SequenceEqual(httpByte))
                            {
                                return index;
                            }
                            return 0;
                        }
                    }
                }
            }
            return 0;
        }


    }
}
