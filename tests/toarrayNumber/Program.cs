using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace invokeSpeed
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //short  int   long double 
            var summary = BenchmarkRunner.Run<Test>();
            Console.ReadLine();
        }
    }


    [MemoryDiagnoser]
    public unsafe class Test
    {
        byte[] arr = new byte[8];
        int number4 = 1;
        long number8 = 1;

        [Benchmark]
        public void ArrayCopy4()
        {
            for (int i = 0; i < 100; i++)
            {
                var source = BitConverter.GetBytes(number4);
                Array.Copy(arr, 0, source, 0, source.Length);
            }
        }
        [Benchmark]
        public void ArrayCopy8()
        {
            for (int i = 0; i < 100; i++)
            {
                var source = BitConverter.GetBytes(number8);
                Array.Copy(arr, 0, source, 0, source.Length);
            }
        }

        [Benchmark]
        public void SpanCopy4()
        {
            for (int i = 0; i < 100; i++)
            {
                BitConverter.GetBytes(number4).AsSpan().CopyTo(arr);
            }
        }
        [Benchmark]
        public void SpanCopy8()
        {
            for (int i = 0; i < 100; i++)
            {
                BitConverter.GetBytes(number8).AsSpan().CopyTo(arr);
            }
        }

        [Benchmark]
        public void UnSafeSpanCopy4()
        {
            for (int i = 0; i < 100; i++)
            {
                ref int v = ref number4;
                fixed (void* p = &v)
                {
                    new Span<byte>(p, 4).CopyTo(arr);
                }
            }
        }
        [Benchmark]
        public void UnSafeSpanCopy8()
        {
            for (int i = 0; i < 100; i++)
            {
                ref long v = ref number8;
                fixed (void* p = &v)
                {
                    new Span<byte>(p, 8).CopyTo(arr);
                }
            }
        }


        [Benchmark]
        public void UnSafe4()
        {
            for (int i = 0; i < 100; i++)
            {
                ref int v = ref number4;
                fixed (void* p = &v)
                {
                    arr[0] = *((byte*)p);
                    arr[1] = *((byte*)p + 1);
                    arr[2] = *((byte*)p + 2);
                    arr[3] = *((byte*)p + 3);
                }
            }
        }
        [Benchmark]
        public void UnSafe8()
        {
            for (int i = 0; i < 100; i++)
            {
                ref long v = ref number8;
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