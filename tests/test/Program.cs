using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace invokeSpeed
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ushort[] res = "123,1/4,2/5".Split(',').SelectMany(c =>
            {
                string[] arr = c.Split('/');
                if (arr.Length == 1) return new ushort[1] { ushort.Parse(arr[0]) };

                ushort start = ushort.Parse(arr[0]);
                ushort end = ushort.Parse(arr[1]);
                ushort[] result = new ushort[(end - start) + 1];
                for (ushort p = start, i = 0; p <= end; p++, i++)
                {
                    result[i] = p;
                }
                return result;
            }).ToArray();
            Console.WriteLine(string.Join(",",res));
            //var summary = BenchmarkRunner.Run<Test>();
        }


    }



    [MemoryDiagnoser]
    public unsafe class Test
    {
        [Benchmark]
        public void Test1()
        {
            Span<byte> span = Guid.NewGuid().ToByteArray().AsSpan(0, 8);
            string str = BitConverter.ToUInt64(span).ToString();
            ulong id = ulong.Parse(str.Substring(0, 9));
        }
    }
}