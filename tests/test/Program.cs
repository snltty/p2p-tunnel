using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using common.libs.extends;
using System.Net;
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
        }

        string a = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
        byte[] bytes = new byte[1024];

        [Benchmark]
        public void UTF8()
        {
            for (int i = 0; i < 100; i++)
                Encoding.UTF8.GetBytes(a).CopyTo(bytes, 0);
        }


        [Benchmark]
        public void UTF8New()
        {
            for (int i = 0; i < 100; i++)
                a.AsSpan().ToUTF8Bytes(bytes);
        }

        [Benchmark]
        public void UTF16()
        {
            for (int i = 0; i < 100; i++)
                a.GetUTF16Bytes().CopyTo(bytes);

        }
    }
}