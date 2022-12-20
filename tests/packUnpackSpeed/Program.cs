using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using common.libs;
using common.libs.extends;
using common.server.model;
using common.socks5;
using System.Net;
using System.Net.Sockets;

namespace packUnpackSpeed
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<Test>();
            Console.ReadLine();
        }
    }

    [MemoryDiagnoser]
    public class Test
    {


        [GlobalSetup]
        public void Setup()
        {

        }


        ReceiveDataBuffer buffer = new ReceiveDataBuffer();
        Socks5Info socks5 = new Socks5Info
        {
            Id = 1,
            Data = new byte[1024],
            Socks5Step = Socks5EnumStep.Forward,
            TargetEP = new IPEndPoint(IPAddress.Any, 0),
            SourceEP = new IPEndPoint(IPAddress.Any, 0)
        };
        Socks5Info socks5Debytes = new Socks5Info();
        MessageRequestWrap messageWrapDebytes = new MessageRequestWrap();
        byte[] data = new byte[32 * 1024];


        [Benchmark]
        public void Test_32KB()
        {
            Run(data, 32 * 1024);
        }

        [Benchmark]
        public void Test_1KB()
        {
            Run(data, 1024);
        }
        [Benchmark]
        public void Test_2KB()
        {
            Run(data, 2 * 1024);
        }
        [Benchmark]
        public void Test_4KB()
        {
            Run(data, 4 * 1024);
        }
        [Benchmark]
        public void Test_8KB()
        {
            Run(data, 8 * 1024);
        }
        [Benchmark]
        public void Test_16KB()
        {
            Run(data, 16 * 1024);
        }

        private void Run(byte[] data, int len)
        {
            int times = (1 * 1024 * 1024 * 1024) / len;
            for (int i = 0; i < times; i++)
            {
                socks5.Data = data.AsMemory(0, len);
                //序列化
                var bytes = socks5.ToBytes(out int length);
                //打包
                MessageRequestWrap request = new MessageRequestWrap
                {
                    MessengerId = 1,
                    RequestId = 1,
                    Payload = bytes.AsMemory(0, length)
                };
                var requestBytes = request.ToArray(out length);

                //粘包
                buffer.AddRange(requestBytes, 0, length);
                do
                {
                    int packageLen = buffer.Data.Span.ToInt32();
                    if (packageLen > buffer.Size - 4)
                    {
                        break;
                    }

                    //去掉4个字节的长度头
                    var readReceive = buffer.Data.Slice(0, packageLen + 4).Slice(4);
                    //解包
                    messageWrapDebytes.FromArray(readReceive);
                    //反序列化
                    socks5Debytes.DeBytes(messageWrapDebytes.Payload);


                    buffer.RemoveRange(0, packageLen + 4);
                } while (buffer.Size > 4);

                socks5.Return(bytes);
                request.Return(requestBytes);
            }
        }

    }
}