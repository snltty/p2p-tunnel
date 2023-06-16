using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using common.libs;
using common.proxy;
using common.server;
using common.server.model;
using Iced.Intel;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace test
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ulong[] array = new ulong[4] { ulong.MaxValue, ulong.MaxValue, ulong.MaxValue, 0x7Fffffffffffffff };
            for (byte groupIndex = 0; groupIndex < array.Length; groupIndex++)
            {
                ulong group = array[groupIndex];
                if (group < ulong.MaxValue)
                {
                    Console.WriteLine($"group:{groupIndex},{group},{Convert.ToString((uint)((group >> 32) & uint.MaxValue), 2)}{Convert.ToString((uint)(group& uint.MaxValue), 2)}");
                    for (byte byteIndex = 0; byteIndex < 8; byteIndex++)
                    {
                        byte _byte = (byte)((group >> byteIndex*8) & 0b11111111);
                        Console.WriteLine($"byte:{byteIndex},{Convert.ToString(_byte,2)}");
                        if (_byte < byte.MaxValue)
                        {
                            for (byte bitIndex = 0; bitIndex < 8; bitIndex++)
                            {
                                byte bit = (byte)((_byte >> bitIndex) & 0b1);
                                if (bit == 0)
                                {
                                    Console.WriteLine($"bit:{bitIndex},{bit}");
                                    Console.WriteLine(groupIndex * 64 + byteIndex * 8 + bitIndex);
                                    return;
                                }
                            }
                        }

                    }
                    Console.WriteLine("===================================");
                }

            }
            //BenchmarkRunner.Run<Test>();
        }
    }

    [MemoryDiagnoser]
    public partial class Test
    {
        [GlobalSetup]
        public void Startup()
        {
            //config.ParseFirewall();
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
    class TestInfo : ITestInfo
    {
        public void Test()
        {

        }
    }
}