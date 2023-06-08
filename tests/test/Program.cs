using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using common.libs;
using common.proxy;
using common.server;
using common.server.model;
using System.Buffers;
using System.Net;
using System.Text;

namespace test
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //BenchmarkRunner.Run<Test>();
        }
    }

    [MemoryDiagnoser]
    public partial class Test
    {
        [GlobalSetup]
        public void Startup()
        {
            config.ParseFirewall();
        }


        byte[] requestData = new byte[1024];
        private MessageRequestWrap messageRequestWrap = new MessageRequestWrap { };
        [Benchmark]
        public void Request_PackUnPack()
        {
            messageRequestWrap.Payload = requestData.AsMemory();
            byte[] bytes = messageRequestWrap.ToArray(out int length);
            messageRequestWrap.FromArray(bytes.AsMemory(0, length));
            messageRequestWrap.Return(bytes);
        }


        byte[] data = Encoding.UTF8.GetBytes("GET /AAA/AAA HTTP/1.1\r\nHost:www.baidu.com");
        [Benchmark]
        public void HttpParser_IsHttp()
        {
            int index = HttpParser.IsHttp(data);
        }


        ProxyInfo info = new ProxyInfo { Data = new byte[1024], TargetAddress = new byte[] { 0, 0, 0, 0 }, TargetPort = 8080, PluginId = 1 };
        Config config = new Config
        {
            Firewall = new List<FirewallItem> {
                new FirewallItem { ID = 1, IP = new string[] { "0.0.0.0/0" }, PluginId=1, Port="0", Protocol= FirewallProtocolType.TCP_UDP, Type= FirewallType.Allow }
            }
        };
        [Benchmark]
        public void Proxy_PackUnPack()
        {
            info.Data = requestData.AsMemory();
            byte[] bytes = info.ToBytes(out int length);
            info.DeBytes(bytes.AsMemory(0, length));
            info.Return(bytes);
        }
        [Benchmark]
        public void Proxy_FirewallDenied()
        {
            config.FirewallDenied(info);
        }

    }
}