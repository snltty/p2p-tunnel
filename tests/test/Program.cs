using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using common.libs;
using common.libs.extends;
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
            Console.WriteLine(string.Join(",", Encoding.UTF8.GetBytes("80-443")));

           // FindPoerTest t = new FindPoerTest();
           // Console.WriteLine(t.Exists(t.array,253));
          //  t.Add(t.array,253);
           // Console.WriteLine(t.Exists(t.array,253));
            // BenchmarkRunner.Run<Test>();
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


        FindPoerTest t = new FindPoerTest();
        [Benchmark]
        public void FindIP()
        {
            t.Find(t.array, out byte point);
        }
        [Benchmark]
        public void AddIP()
        {
            t.Add(t.array, 253);
        }
        [Benchmark]
        public void DeleteIP()
        {
            t.Delete(t.array, 253);
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

    class FindPoerTest
    {
        public ulong[] array = new ulong[4] { ulong.MaxValue, ulong.MaxValue, ulong.MaxValue, 0 };
        /// <summary>
        /// 查找网段内可用ip(/24)
        /// </summary>
        /// <param name="array">缓存数组</param>
        /// <param name="value">找到的值</param>
        /// <returns></returns>
        /// <exception cref="Exception">array length must be 4</exception>
        public bool Find(ulong[] array, out byte value)
        {
            value = 0;
            if (array.Length != 4) throw new Exception("array length must be 4");
            //排除 .1 .255 .256
            array[0] |= 0b1;
            array[3] |= (ulong)0b11 << 62;

            if (array[0] < ulong.MaxValue) value = Find(array[0], 0);
            else if (array[1] < ulong.MaxValue) value = Find(array[1], 1);
            else if (array[2] < ulong.MaxValue) value = Find(array[2], 2);
            else if (array[3] < ulong.MaxValue) value = Find(array[3], 3);
            return value > 0;
        }
        private byte Find(ulong group, byte index)
        {
            byte value = (byte)(index * 64);
            //每次对半开，也可以循环，循环稍微会慢3-4ns，常量值快一点
            ulong _group = (group & uint.MaxValue);
            if (_group >= uint.MaxValue) { _group = group >> 32; value += 32; }
            group = _group;

            _group = (group & ushort.MaxValue);
            if (_group >= ushort.MaxValue) { _group = group >> 16; value += 16; }
            group = _group;

            _group = (group & byte.MaxValue);
            if (_group >= byte.MaxValue) { _group = group >> 8; value += 8; }
            group = _group;

            _group = (group & 0b1111);
            if (_group >= 0b1111) { _group = group >> 4; value += 4; }
            group = _group;

            _group = (group & 0b11);
            if (_group >= 0b11) { _group = group >> 2; value += 2; }
            group = _group;

            _group = (group & 0b1);
            if (_group >= 0b1) { value += 1; }
            value += 1;

            return value;
        }
        /// <summary>
        /// 是否存在一个ip
        /// </summary>
        /// <param name="group"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public bool Exists(ulong[] array, byte value)
        {
            if (array.Length != 4) throw new Exception("array length must be 4");
            int arrayIndex = value / 64;
            int length = value - arrayIndex * 64;

            return (array[arrayIndex] >> (length - 1) & 1) == 1;
        }
        /// <summary>
        /// 将一个ip(/24)设为已使用
        /// </summary>
        /// <param name="array">缓存数组</param>
        /// <param name="value">值</param>
        public void Add(ulong[] array, byte value)
        {
            if (array.Length != 4) throw new Exception("array length must be 4");
            int arrayIndex = value / 64;
            int length = value - arrayIndex * 64;
            array[arrayIndex] |= (ulong)1 << (length - 1);
        }
        /// <summary>
        /// 删除一个ip(/24),设为未使用
        /// </summary>
        /// <param name="array"></param>
        /// <param name="value"></param>
        /// <exception cref="Exception"></exception>
        public void Delete(ulong[] array, byte value)
        {
            if (array.Length != 4) throw new Exception("array length must be 4");
            int arrayIndex = value / 64;
            int length = value - arrayIndex * 64;
            array[arrayIndex] &= ~((ulong)1 << (length - 1));
        }
    }
}