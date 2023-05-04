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
            using Ping ping = new Ping();
            var result = ping.Send("www.baidu.com", 1000, Array.Empty<byte>(), new PingOptions
            {
                Ttl = 2
            });
            Console.WriteLine(result.Status.ToString());
        }


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