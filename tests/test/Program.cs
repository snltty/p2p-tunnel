using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using common.libs;
using common.proxy;
using common.server;
using common.server.model;
using Iced.Intel;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace test
{
    internal class Program
    {
        static void Main(string[] args)
        {

            BenchmarkRunner.Run<Test>();
        }
    }

    [MemoryDiagnoser]
    public partial class Test
    {
        [GlobalSetup]
        public void Startup()
        {
            //config.ParseFirewall();
            voidMethod = (VoidDelegate)Delegate.CreateDelegate(typeof(VoidDelegate), info1, info1.GetType().GetMethod("Test"));
        }

        delegate void VoidDelegate();
        VoidDelegate voidMethod;
        TestInfo info1 = new TestInfo();
        ITestInfo info2 = new TestInfo();

        [Benchmark]
        public void Info1()
        {
            for (int i = 0; i < 100; i++)
            {
                voidMethod.Invoke();
            }
        }
        [Benchmark]
        public void Info2()
        {
            for (int i = 0; i < 100; i++)
            {
                info2.Test();
            }
        }

        #region 隐藏



        byte[] requestData = new byte[1024];
        private MessageRequestWrap messageRequestWrap = new MessageRequestWrap { };
        /*
        [Benchmark]
        public void Request_PackUnPack()
        {
            messageRequestWrap.Payload = requestData.AsMemory();
            byte[] bytes = messageRequestWrap.ToArray(out int length);
            messageRequestWrap.FromArray(bytes.AsMemory(0, length));
            messageRequestWrap.Return(bytes);
        }
        */

        byte[] data = Encoding.UTF8.GetBytes("GET /AAA/AAA HTTP/1.1\r\nHost:www.baidu.com");
        /*
        [Benchmark]
        public void HttpParser_IsHttp()
        {
            int index = HttpParser.IsHttp(data);
        }
        */

        ProxyInfo info = new ProxyInfo { Data = new byte[1024], TargetAddress = new byte[] { 0, 0, 0, 0 }, TargetPort = 8080, PluginId = 1 };
        Config config = new Config
        {
            Firewall = new List<FirewallItem> {
                new FirewallItem { ID = 1, IP = new string[] { "0.0.0.0/0" }, PluginId=1, Port="0", Protocol= FirewallProtocolType.TCP_UDP, Type= FirewallType.Allow }
            }
        };
        /*
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
        */
        #endregion
    }

    interface ITestInfo
    {
        public void Test();
    }
    class TestInfo: ITestInfo
    {
        public void Test()
        {

        }
    }
}