using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using common.libs;
using common.libs.extends;
using Iced.Intel;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
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
            //yte[] data = Encoding.UTF8.GetBytes("GET /AAA/AAA?sdfsgfgergergt=egt=ert=ret==yte=yrt=yr=tyr=tyh=rth=rth=r=j=tr=jhr=jhrt=y=tr=r=ur=utr=uty=uty=ujyr=jytr=j= HTTP/1.1\r\nHost:www.baidu.com");
            // int index = HttpParser.IsHttp(data);
            var summary = BenchmarkRunner.Run<Test>();
        }
    }



    [MemoryDiagnoser]
    public unsafe class Test
    {
        byte[] data = Encoding.UTF8.GetBytes("GET /AAA/AAA HTTP/1.1\r\nHost:www.baidu.com");

        [GlobalSetup]
        public void Startup()
        {

        }

        [Benchmark]
        public void Test1()
        {
            Encoding.UTF8.GetString(data);
            // int index = HttpParser.IsHttp(data);
        }

    }
}