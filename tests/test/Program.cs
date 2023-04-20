using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace invokeSpeed
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string http = "CONNECT www.mydomain.com:443 HTTP/1.1\r\nHost: 127.0.0.1:8080\r\nProxy-Connection: keep-alive\r\nUser-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/112.0.0.0 Safari/537.36\r\n\r\n";
            var bytes = Encoding.UTF8.GetBytes(http);

            int portStart = -1;
            Span<byte> span = GetHost(bytes, ref portStart);

            Console.WriteLine(Encoding.UTF8.GetString(span));

            Console.WriteLine(portStart);
            Console.WriteLine(Encoding.UTF8.GetString(span.Slice(0, portStart)));
            Console.WriteLine(Encoding.UTF8.GetString(span.Slice(portStart+1)));

            Console.ReadLine();
        }

        private static byte[] hostBytes = Encoding.ASCII.GetBytes("host: ");
        /// <summary>
        /// 从http报文中获取host
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        public static Span<byte> GetHost(in Span<byte> span, ref int portStart)
        {
            int keyIndex = -1, start = 0;
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
                        //因为 headers 是从第二行开始，所以，可以在碰到每个\n时，向后获取与目标key相同长度的内容与之匹配，成功则已找到key，标识位置
                        Span<byte> keySpan = span.Slice(i + 1, hostBytes.Length);
                        keySpan[0] |= 32;
                        if (keySpan[0] == hostBytes[0] && keySpan.SequenceEqual(hostBytes))
                        {
                            keyIndex = i + 1 + hostBytes.Length;
                            i += hostBytes.Length - 1;
                            start = i;
                        }
                    }

                }
                //找到key之后，如果碰到了\n，就说明value内容已结束，截取key的位置到当前\n位置，就是值内容
                else if (span[i] == 10)
                {
                    Span<byte> valueSpan = span.Slice(keyIndex, i - 1 - keyIndex);
                    return valueSpan;
                }
                else if (span[i] == 58)
                {
                    portStart = i - keyIndex;
                }
            }
            return Array.Empty<byte>();
        }
    }
}