using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using common.libs;
using common.libs.extends;
using Iced.Intel;
using Microsoft.Diagnostics.Tracing.Parsers.FrameworkEventSource;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;

namespace invokeSpeed
{
    internal class Program
    {
        static void Main(string[] args)
        {
           Console.WriteLine(Uri.EscapeDataString("1ad;.',&%$@!中文"));
           // var summary = BenchmarkRunner.Run<Test>();
        }
    }



    [MemoryDiagnoser]
    public unsafe class Test
    {
        byte[] data = Encoding.UTF8.GetBytes("HEAD /AAA/AAA?tytryrhfghbhfghfhf5454f5hbfdgregregre=fgfdewdfdfdfertggregregreeretergfger HTTP/1.1\r\nHost:www.baidu.com");
        StringBuilder sb = new StringBuilder();

        [GlobalSetup]
        public void Startup()
        {

        }

        [Benchmark]
        public void Test1()
        {
            
            sb.Clear();
            sb.Append("HEAD /AAA/AAA?tytryrhfghbhfghfhf5454f5hbfdgregregre=fgfdewdfdfdfertggregregreeretergfger HTTP/1.1\r\nHost:www.baidu.com");
           // int index = HttpParser.IsHttp(data);
        }

    }
}