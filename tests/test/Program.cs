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

namespace invokeSpeed
{
    internal class Program
    {
        static void Main(string[] args)
        {
            
            IPAddress ip = IPAddress.Parse("192.168.1.3");
            uint intip = BinaryPrimitives.ReadUInt32BigEndian(ip.GetAddressBytes());
            Console.WriteLine(intip);

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