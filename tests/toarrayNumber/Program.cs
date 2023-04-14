using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace invokeSpeed
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
    public unsafe class Test
    {
        byte[] arr = new byte[8];
        long number = 1;

        /// <summary>
        /// 数组拷贝
        /// </summary>
        [Benchmark]
        public void ArrayCopy()
        {
            for (int i = 0; i < 100; i++)
            {
                var source = BitConverter.GetBytes(number);
                Array.Copy(arr, 0, source, 0, source.Length);
            }
        }

        /// <summary>
        /// span拷贝
        /// </summary>
        [Benchmark]
        public void SpanCopy()
        {
            for (int i = 0; i < 100; i++)
            {
                BitConverter.GetBytes(number).AsSpan().CopyTo(arr);
            }
        }

        /// <summary>
        /// 指针span拷贝
        /// </summary>
        [Benchmark]
        public void UnSafeSpanCopy()
        {
            for (int i = 0; i < 100; i++)
            {
                ref long v = ref number;
                fixed (void* p = &v)
                {
                    new Span<byte>(p, 8).CopyTo(arr);
                }
            }
        }

        /// <summary>
        /// 指针赋值
        /// </summary>
        [Benchmark]
        public void UnSafe()
        {
            for (int i = 0; i < 100; i++)
            {
                ref long v = ref number;
                fixed (void* p = &v)
                {
                    arr[0] = *((byte*)p);
                    arr[1] = *((byte*)p + 1);
                    arr[2] = *((byte*)p + 2);
                    arr[3] = *((byte*)p + 3);
                    arr[4] = *((byte*)p + 4);
                    arr[5] = *((byte*)p + 5);
                    arr[6] = *((byte*)p + 6);
                    arr[7] = *((byte*)p + 7);
                }
            }
        }
    }
}