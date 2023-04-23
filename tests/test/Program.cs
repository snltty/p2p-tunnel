using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace invokeSpeed
{
    internal class Program
    {
        static void Main(string[] args)
        {
            /*
            uint mask = BinaryPrimitives.ReadUInt32BigEndian(IPAddress.Parse("10.15.17.104").GetAddressBytes());
            uint mask1 = 0xffffffff << (32 - 29) ;
            mask &= mask1;

            for (int i = 0; i <= 255; i++)
            {
                uint ip = BinaryPrimitives.ReadUInt32BigEndian(IPAddress.Parse($"10.15.17.{i}").GetAddressBytes());
                if(mask == (ip & mask1))
                {
                    Console.WriteLine($"10.15.17.{i}");
                }
            }

            */
            /*
            IPAddress iPAddress = IPAddress.Parse("192.168.54.2");
            uint ip = BinaryPrimitives.ReadUInt32BigEndian(iPAddress.GetAddressBytes());
            Console.WriteLine(Convert.ToString(ip,2));
            Console.WriteLine(new IPAddress(BitConverter.GetBytes(ip)).ToString());
            */
            Console.WriteLine(1<<(byte)7);

            Console.ReadLine();
        }

    }
}