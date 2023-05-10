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

            Dictionary<string, string> Args = new Dictionary<string, string>();
            Args["aa"] = "aa";
            Console.Write(Args["aa"]);
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