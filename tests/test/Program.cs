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
            List<ulong> list = new List<ulong>();
            Random rd = new Random();
            for (int i = 0; i < 10000000; i++)
            {
                string str = BitConverter.ToUInt64(Guid.NewGuid().ToByteArray()).ToString();
                ulong val = ulong.Parse(str.Substring(str.Length-15, 15));
                //if (list.Contains(val) == false) list.Add(val);
            }
            Console.WriteLine(list.Count);
            
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