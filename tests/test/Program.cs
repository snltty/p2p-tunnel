using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using common.libs;
using common.libs.extends;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Mail;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace invokeSpeed
{
    internal class Program
    {
        static void Main(string[] args)
        {

            string str = "GET /systembc/password.php HTTP/1.0";

            var bytes = Encoding.UTF8.GetBytes(str);

            int port = 0;
            GetHost(bytes,ref port);


            /*
            byte maskLength = 19;
            uint maskValue = 0xffffffff << (32 - maskLength);

            uint netWork = ip & maskValue;
            uint gb = ip | (~maskValue);

            Console.WriteLine($"网络号:{string.Join(",", BitConverter.GetBytes(netWork).Reverse())}");
            Console.WriteLine($"广播:{string.Join(",", BitConverter.GetBytes(gb).Reverse())}");
            */

            //var summary = BenchmarkRunner.Run<Test>();
        }


        private static byte[] hostBytes = Encoding.ASCII.GetBytes("host: ");
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
                        //两个换行，headers已结束
                        if (i + 1 + hostBytes.Length >= span.Length || i+1>=span.Length || span[i + 1] == 10)
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


    }

    public sealed class VeaLanIPAddressOnLine
    {
        public Dictionary<uint, VeaLanIPAddressOnLineItem> Items { get; set; } = new Dictionary<uint, VeaLanIPAddressOnLineItem>();

        public byte[] ToBytes()
        {
            if (Items.Count == 0) return Helper.EmptyArray;

            MemoryStream memoryStream = new MemoryStream();
            byte[] keyBytes = new byte[4];
            foreach (var item in Items)
            {
                item.Key.ToBytes(keyBytes);
                memoryStream.Write(keyBytes, 0, keyBytes.Length);

                memoryStream.WriteByte((byte)(item.Value.Online ? 1 : 0));

                ReadOnlySpan<byte> name = item.Value.Name.GetUTF16Bytes();
                memoryStream.WriteByte((byte)name.Length);
                memoryStream.WriteByte((byte)item.Value.Name.Length);
                memoryStream.Write(name);
            }

            return memoryStream.ToArray();
        }

        public void DeBytes(ReadOnlyMemory<byte> memory)
        {
            if (memory.Length == 0) return;

            ReadOnlySpan<byte> span = memory.Span;

            int index = 0;
            while (index < memory.Length)
            {
                uint key = span.Slice(index).ToUInt32();
                index += 4;

                bool online = span[index] == 1;
                index += 1;

                string name = span.Slice(index + 2, span[index]).GetUTF16String(span[index + 1]);
                index += 1 + 1 + span[index];

                Items[key] = new VeaLanIPAddressOnLineItem { Online = online, Name = name };
            }
        }

    }

    public sealed class VeaLanIPAddressOnLineItem
    {
        public bool Online { get; set; }
        public string Name { get; set; } = string.Empty;
    }



    [MemoryDiagnoser]
    public unsafe class Test
    {
        [Benchmark]
        public void Test1()
        {
        }
    }
}