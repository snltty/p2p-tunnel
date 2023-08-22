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
            Console.WriteLine(string.Join(",",BitConverter.GetBytes((ushort)53)));
           //BenchmarkRunner.Run<Test>();
        }
    }

    [MemoryDiagnoser]
    public unsafe partial class Test
    {
        [GlobalSetup]
        public void Startup()
        {
        }

        byte[] bytes = new byte[] { 0, 0, 48, 57 };

        [Benchmark]
        public unsafe void Test1()
        {
            fixed (byte* p = &bytes[0])
            {
                ushort port1 = (ushort)((*(p + 2) << 8 & 0xFF00) | *(p + 3));
            }
        }

        [Benchmark]
        public void Test2()
        {
            ushort port = BinaryPrimitives.ReverseEndianness(bytes.AsSpan(2, 2).ToUInt16());
        }

    }
}