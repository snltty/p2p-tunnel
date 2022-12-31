using System.Linq;

namespace socks5
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string str = "11-22-33-44-55-66的";

            var bytes = str.ToUTF16Bytes();
            var bytes2 = str.GetUTF16Bytes();
            Console.WriteLine(string.Join(",",bytes));
            Console.WriteLine(string.Join(",", bytes2.ToArray()));

            Console.WriteLine(bytes.AsSpan().GetUTF16String());

            Console.ReadLine();
        }
    }

    public class ConnectionInfo
    {
        /// <summary>
        /// 客户端唯一id
        /// </summary>
        public ulong Id { get; set; }
    }
}