using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;

namespace udp2tcp
{
    internal class Program
    {
        static void Main(string[] args)
        {

            Udp();
            Server();
            Client();
            Console.ReadLine();
        }

        static void Client()
        {
            Task.Run(() =>
            {
                try
                {
                    TcpClient tcp = new TcpClient();
                    tcp.Connect(IPAddress.Loopback, 5000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex + "");
                }
            });
        }

        static void Udp()
        {
            Task.Run(() =>
            {

                try
                {
                    var rawTcp = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.IP);
                    rawTcp.Bind(new IPEndPoint(IPAddress.Any, 5000));

                    var bytes = new byte[1024];
                    int len = rawTcp.Receive(bytes);
                    Console.WriteLine($"IP数据包:{string.Join(",", bytes.AsMemory(0, len).ToArray())}");

                    int headLength = bytes[0] & 0b00001111;
                    Console.WriteLine($"数据包首部长度:{headLength}");

                    Memory<byte> ports = bytes.AsMemory(headLength * 4).Slice(2, 2);
                    int port = BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt16(ports.Span));
                    Console.WriteLine($"TCP包目标端口:{port}");

                    var newPorts = BitConverter.GetBytes(6000);
                    ports.Span[0] = newPorts[1];
                    ports.Span[1] = newPorts[0];
                    int newPort = BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt16(ports.Span));
                    Console.WriteLine($"TCP包新目标端口:{newPort}");

                    var rawTcp1 = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.IP);
                    rawTcp1.SendTo(bytes.AsMemory(0,len).Span,new IPEndPoint(IPAddress.Loopback,6000));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex + "");
                }

                /*
                UdpClient udp = new UdpClient(5000);

                Dictionary<IPEndPoint, Socket> clients = new Dictionary<IPEndPoint, Socket>();
                IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                while (true)
                {
                    byte[] bytes = udp.Receive(ref ep);

                    if (clients.TryGetValue(ep, out Socket socket) == false)
                    {
                        IPEndPoint _ep = ep;
                        socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Tcp);
                        clients.Add(ep, socket);
                        socket.SendTo(bytes, new IPEndPoint(IPAddress.Loopback, 6000));

                        Task.Run(() =>
                        {
                            var bytes = new byte[64 * 1024];
                            while (true)
                            {
                                int len = socket.Receive(bytes);
                                udp.Send(bytes.AsMemory(0, len).Span, _ep);
                            }
                        });
                    }
                    else
                    {
                        socket.SendTo(bytes, new IPEndPoint(IPAddress.Loopback, 6000));
                    }
                }*/
            });
        }

        static void Server()
        {
            Task.Run(() =>
            {
                TcpListener tcp = new TcpListener(IPAddress.Any, 6000);
                tcp.Start();
                while (true)
                {
                    var socket = tcp.AcceptSocket();
                    Task.Run(() =>
                    {
                        var bytes = new byte[64 * 1024];
                        while (true)
                        {
                            int len = socket.Receive(bytes);
                            Console.WriteLine(string.Join(",", bytes.AsMemory(0, len).ToArray()));
                        }
                    });
                }

            });
        }
    }
}