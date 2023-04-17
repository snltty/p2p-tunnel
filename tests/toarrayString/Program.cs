using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using common.libs.extends;
using System.Runtime.InteropServices;
using System.Text.Unicode;

namespace toarrayString
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
        byte[] arr = new byte[1024];
        string str = "dfgdfgfdgdfgdgergergbdfberbrterhrtyjrthrtnrthntrnr让他湖人和概括来讲温柔国家二级给如果i通过热天故热i徒惹i哦腿肉铁人 提瑞图ir太热iu他热提u热iu他ire他";

        /// <summary>
        /// 慢，但是通用，如果与已存在的功能对接，比如解析http协议，就用这个
        /// </summary>
        [Benchmark]
        public void Encoding()
        {
            for (int i = 0; i < 100; i++)
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(str);
                bytes.Length.ToBytes(arr);
                bytes.AsSpan().CopyTo(arr.AsSpan(4));
            }
        }

        /// <summary>
        /// 比较快，但是中文字符比UTF16更大，内容比较长，且大多是ASCII，则可以用这个，
        /// </summary>
        [Benchmark]
        public void UTF8()
        {
            for (int i = 0; i < 100; i++)
            {
                var source = str.AsSpan();
                int utf16Length = source.Length;

                Utf8.FromUtf16(str, arr.AsSpan(8), out _, out int utf8Length, replaceInvalidSequences: false);
                utf16Length.ToBytes(arr);
                utf8Length.ToBytes(arr.AsMemory(4));
            }
        }

        /// <summary>
        /// 非常快，但是，ASCII 字符的大小将是原来的两倍，内容较短，或者大多是中文的，则可以用这个
        /// </summary>
        [Benchmark]
        public void UTF16()
        {
            for (int i = 0; i < 100; i++)
            {
                var source = MemoryMarshal.AsBytes(str.AsSpan());
                source.CopyTo(arr.AsSpan(4));
                str.Length.ToBytes(arr);
            }
        }

    }
}