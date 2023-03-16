using System.Buffers.Binary;
using System.Linq;
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
                    rawTcp.Bind(new IPEndPoint(IPAddress.Loopback, 5000));
                    SetSocketOption(rawTcp);
                    
                    var bytes = new byte[65536];
                    while (true)
                    {
                        int len = rawTcp.Receive(bytes);
                        /*
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

                        //tcp包
                        Memory<byte> tcp = ipPacket.Slice((ipPacketSpan[0] & 0b00001111) * 4);
                        var tcpSpan = tcp.Span;
                        //tcp包来源端口
                        ushort sourcePort = BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt16(tcpSpan));
                        //tcp包目标端口
                        Span<byte> targetPorts = tcpSpan.Slice(2);
                        ushort targetPort = BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt16(targetPorts));
                        //重写tcp目标端口
                        byte[] newPorts = BitConverter.GetBytes(6000);
                        //targetPorts[0] = newPorts[1];
                        //targetPorts[1] = newPorts[0];
                        //校验和
                        ushort checkSum = BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt16(tcpSpan.Slice(16, 2)));

                        IPEndPoint sourceEp = new IPEndPoint(new IPAddress(sourceIp.Span), sourcePort);
                        if (dic.TryGetValue(sourceEp, out Socket socket) == false)
                        {
                            socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.IP);
                            dic.Add(sourceEp, socket);

                            //SetSocketOption(socket);
                            socket.SendTo(ipPacketSpan, new IPEndPoint(IPAddress.Loopback, 6000));
                          
                            Task.Run(() =>
                            {
                                var bytes = new byte[65536];
                                while (true)
                                {
                                    int length = socket.Receive(bytes);

                                    var ipPacket = bytes.AsMemory(0, len);
                                    var ipPacketSpan = ipPacket.Span;

                                    //tcp包
                                    Memory<byte> tcp = ipPacket.Slice((ipPacketSpan[0] & 0b00001111) * 4);
                                    var tcpSpan = tcp.Span;
                                    //来源端口
                                    ushort sourcePort = BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt16(tcpSpan));
                                    //目标端口
                                    ushort targetPort = BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt16(tcpSpan));
                                    //重写目标端口
                                   // var newPorts = BitConverter.GetBytes(sourceEp.Port);
                                    //tcpSpan[0] = newPorts[1];
                                   // tcpSpan[1] = newPorts[0];
                                   // rawTcp.SendTo(ipPacket.Span, sourceEp);
                                }
                            });
                        }
                        else
                        {
                            socket.SendTo(ipPacketSpan, new IPEndPoint(IPAddress.Loopback, 6000));
                        }
                        */
                    }
                    
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex + "");
                }
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

        const int SIO_RCVALL = unchecked((int)0x98000001);//监听所有的数据包
        static bool SetSocketOption(Socket socket) //设置raw socket 
        {
            bool ret_value = true;
            try
            {
                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.HeaderIncluded, 1);
                byte[] IN = new byte[4] { 1, 0, 0, 0 };
                byte[] OUT = new byte[4];

                //低级别操作模式,接受所有的数据包，这一步是关键，必须把socket设成raw和IP Level才可用　　SIO_RCVALL 
                int ret_code = socket.IOControl(SIO_RCVALL, IN, OUT);
                ret_code = OUT[0] + OUT[1] + OUT[2] + OUT[3];//把4个8位字节合成一个32位整数 
                if (ret_code != 0) ret_value = false;
            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex+"");
                ret_value = false;
            }
            return ret_value;
        }
    }
}