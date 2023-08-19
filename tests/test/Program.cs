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

    class AAA
    {
        public int A1 { get; set; }
        public int A2 { get; set; }
        public int A3 { get; set; }
        public int A4 { get; set; }
        public int A5 { get; set; }
    }

    [MemoryDiagnoser]
    public partial class Test
    {
        [GlobalSetup]
        public void Startup()
        {
        }

        object aaa = new AAA();
        [Benchmark]
        public void Test1()
        {
            for (int i = 0; i < 100; i++)
            {
                bool res = aaa.GetType() == typeof(AAA);
            }
        }

        [Benchmark]
        public void Test2()
        {
            for (int i = 0; i < 100; i++)
            {
                bool res = aaa is AAA;
            }
        }

    }
}