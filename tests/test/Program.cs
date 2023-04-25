using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace invokeSpeed
{
    internal class Program
    {
        static void Main(string[] args)
        {

            VeaLanIPAddress ip = new VeaLanIPAddress
            {
                 IPAddress = BinaryPrimitives.ReadUInt32BigEndian(IPAddress.Parse("192.168.100.0").GetAddressBytes()),
                 Mask = 21,
            };
            //ResetMask(ip);

            uint network = GetMinNetWork(ip,out byte maskBitLength);
            Console.WriteLine(string.Join(",", BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(network))));
            Console.WriteLine(ip.Mask);
            Console.WriteLine(maskBitLength);


            /*
            VeaLanIPAddress ip = new VeaLanIPAddress
            {
                IPAddress = new byte[] { 192, 168, 0, 0 },
                Mask = 25
            };
            uint network = GetMinNetWork(ip);

            Console.WriteLine(Convert.ToString(network, 2));
            */

            //uint mask = BinaryPrimitives.ReadUInt32BigEndian(IPAddress.Parse("255.255.248.0").GetAddressBytes());
            //Console.WriteLine(Convert.ToString(mask, 2));

            /*
            uint mask = BinaryPrimitives.ReadUInt32BigEndian(IPAddress.Parse("192.168.0.0").GetAddressBytes());
            uint mask1 = 0xffffffff << (32 - 25) ;
            mask &= mask1;

            for (int i = 0; i <= 255; i++)
            {
                uint ip = BinaryPrimitives.ReadUInt32BigEndian(IPAddress.Parse($"192.168.0.{i}").GetAddressBytes());
                if (mask == (ip & mask1))
                {
                    Console.WriteLine($"192.168.0.{i}");
                }

            }
            */
            /*
            IPAddress iPAddress = IPAddress.Parse("192.168.54.2");
            uint ip = BinaryPrimitives.ReadUInt32BigEndian(iPAddress.GetAddressBytes());
            Console.WriteLine(Convert.ToString(ip,2));
            Console.WriteLine(new IPAddress(BitConverter.GetBytes(ip)).ToString());
            */

            /*
            var udp = new UdpClient(new IPEndPoint(IPAddress.Parse("192.168.1.3"),5000));
            udp.Client.EnableBroadcast = true;

            for (int i = 0; i < 10; i++)
            {
                udp.Send(Encoding.UTF8.GetBytes(i.ToString()),new IPEndPoint(IPAddress.Parse("225.0.0.1"),6000));
            }
            */

            Console.ReadLine();
        }


        static void ResetMask(VeaLanIPAddress ip)
        {
            if (ip.Mask == 0)
            {
                ip.Mask = 32;
                for (int i = 0; i < sizeof(uint); i++)
                {
                    if (((ip.IPAddress >> (i * 8)) & 0x000000ff) != 0)
                    {
                        break;
                    }
                    ip.Mask -= 8;
                }
            }
        }

        static uint GetMinNetWork(VeaLanIPAddress ip, out byte maskBitLength)
        {
            uint mask = 0xffffff00;
            maskBitLength = 24;
            for (int i = 1; i < sizeof(uint); i++)
            {
                if (((ip.IPAddress >> (i * 8)) & 0x000000ff) == 0 || maskBitLength > ip.Mask)
                {
                    mask <<= 8;
                    maskBitLength -= 8;
                }
                else break;
            }
            return ip.IPAddress & mask;
        }
    }

    public sealed class VeaLanIPAddress
    {
        public uint IPAddress { get; set; }
        public byte Mask { get; set; }
    }

    public sealed class IPAddressInfo
    {
        /// <summary>
        /// ip 小端
        /// </summary>
        public uint IP { get; set; }
        /// <summary>
        /// 局域网网段
        /// </summary>
        public VeaLanIPAddress[] LanIPs { get; set; }
    }


}