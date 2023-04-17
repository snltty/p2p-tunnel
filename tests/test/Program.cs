using BenchmarkDotNet.Attributes;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace invokeSpeed
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // var summary = BenchmarkRunner.Run<Test>();
            Console.ReadLine();
        }
    }

    [MemoryDiagnoser]
    public unsafe class Test
    {
        int number = 12345;
        byte[] bytes = new byte[4];

        [GlobalSetup]
        public void Setup()
        {
            BitConverter.GetBytes(number).AsSpan().CopyTo(bytes);
        }

        [Benchmark]
        public void ToInt32()
        {
            int v = BitConverter.ToInt32(bytes.AsSpan());
        }

        [Benchmark]
        public void UnSafe()
        {
            int v = Unsafe.As<byte, int>(ref MemoryMarshal.GetReference(bytes.AsSpan()));
        }
    }
}