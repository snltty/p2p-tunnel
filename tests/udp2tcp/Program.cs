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
                    Console.WriteLine("connected");
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

                Dictionary<IPEndPoint, Socket> dic = new Dictionary<IPEndPoint, Socket>();
                try
                {
                    var rawTcp = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.IP);
                    rawTcp.Bind(new IPEndPoint(IPAddress.Any, 5000));

                    var bytes = new byte[65536];
                    while (true)
                    {
                        int len = rawTcp.Receive(bytes);

                        var ipPacket = bytes.AsMemory(0, len);
                        var ipPacketSpan = ipPacket.Span;

                        //只处理tcp
                        int proto = ipPacketSpan[9];
                        if (proto != 6)
                        {
                            continue;
                        }

                        //ip包ip地址
                        Memory<byte> sourceIp = ipPacket.Slice(12, 4);
                        Memory<byte> targetIp = ipPacket.Slice(16, 4);


                        Memory<byte> tcp = ipPacket.Slice((ipPacketSpan[0] & 0b00001111) * 4);
                        var tcpSpan = tcp.Span;
                        //tcp包端口
                        ushort sourcePort = BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt16(tcpSpan));
                        Span<byte> targetPorts = tcpSpan.Slice(2);
                        ushort targetPort = BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt16(targetPorts));
                        //重写tcp目标端口
                        var newPorts = BitConverter.GetBytes(6000);
                        targetPorts[0] = newPorts[1];
                        targetPorts[1] = newPorts[0];

                        targetPort = BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt16(targetPorts));
                        //Console.WriteLine($"目标端口：{targetPort}");

                        IPEndPoint sourceEp = new IPEndPoint(new IPAddress(sourceIp.Span), sourcePort);
                        if (dic.TryGetValue(sourceEp, out Socket socket) == false)
                        {
                            socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.IP);
                            dic.Add(sourceEp, socket);
                            socket.SendTo(ipPacketSpan, new IPEndPoint(IPAddress.Loopback, 6000));
                            Task.Run(() =>
                            {
                                var bytes = new byte[65536];
                                while (true)
                                {
                                    int length = socket.Receive(bytes);

                                    var ipPacket = bytes.AsMemory(0, len);
                                    var ipPacketSpan = ipPacket.Span;

                                    Memory<byte> tcp = ipPacket.Slice((ipPacketSpan[0] & 0b00001111) * 4);
                                    var tcpSpan = tcp.Span;
                                    ushort sourcePort = BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt16(tcpSpan));
                                    // Console.WriteLine($"源端口：{sourcePort}");
                                    ushort targetPort = BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt16(tcpSpan));
                                    //Console.WriteLine($"目标端口1：{targetPort}");

                                    var newPorts = BitConverter.GetBytes(5000);
                                    tcpSpan[0] = newPorts[1];
                                    tcpSpan[1] = newPorts[0];

                                    sourcePort = BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt16(tcpSpan));
                                    // Console.WriteLine($"源端口1：{sourcePort}");

                                    rawTcp.SendTo(ipPacket.Span, sourceEp);


                                    //Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]:receive:{length}");
                                }
                            });
                        }
                        else
                        {
                            socket.SendTo(ipPacketSpan, new IPEndPoint(IPAddress.Loopback, 6000));
                        }
                    }
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