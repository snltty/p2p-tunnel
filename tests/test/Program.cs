using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using common.libs.extends;
using System.Buffers.Binary;
using System.Net;
using System.Text;

namespace test
{
    internal class Program
    {
        static unsafe void Main(string[] args)
        {
            BenchmarkRunner.Run<Test>();
        }
    }

    [MemoryDiagnoser]
    public unsafe partial class Test
    {
        [GlobalSetup]
        public void Startup()
        {
        }

        byte[] bytes = new byte[24];

        [Benchmark]
        public unsafe void Test1()
        {
            var span = bytes.AsSpan();
            ushort addressFamily = BitConverter.ToUInt16(bytes, 0);
            ushort port = BitConverter.ToUInt16(bytes, 2);
            uint ipv4 = BitConverter.ToUInt16(bytes, 4);
            ushort ipv61 = BitConverter.ToUInt16(bytes, 8);
            ushort ipv62 = BitConverter.ToUInt16(bytes, 16);
            TextStruct textStruct = new TextStruct(addressFamily, port, ipv4, ipv61, ipv62);
            TestFunc2(textStruct);
        }

        [Benchmark]
        public void Test2()
        {
            byte[] res = new byte[bytes.Length];
            bytes.AsSpan().CopyTo(res);
            TestFunc(res);
        }

        private unsafe void TestFunc(byte[] arr)
        {
            fixed (void* p = arr)
            {
                ushort port = *((byte*)p + 2);
            }
        }
        private void TestFunc2(TextStruct s)
        {

            ushort port = s.Port;
        }

        public readonly record struct TextStruct
        {
            public TextStruct(ushort addressFamily, ushort port, uint ipv4, ulong ipv61, ulong ipv62)
            {
                AddressFamily = addressFamily;
                Port = port;
                Ipv4 = ipv4;
                Ipv61 = ipv61;
                Ipv62 = ipv62;
            }
            public readonly ushort AddressFamily;
            public readonly ushort Port;
            public readonly uint Ipv4;
            public readonly ulong Ipv61;
            public readonly ulong Ipv62;
        }
    }
}