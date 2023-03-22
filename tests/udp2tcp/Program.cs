using Lidgren.Network;
using PseudoTcp;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using static PseudoTcp.PseudoTcpSocket;

namespace udp2tcp
{
    internal class Program
    {
        static void Main(string[] args)
        {

            Test1();
            Console.ReadLine();
        }

        static void Test2()
        {
            NetPeerConfiguration config = new NetPeerConfiguration("ImageTransfer");
            config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
            config.EnableMessageType(NetIncomingMessageType.DiscoveryRequest);
            config.EnableMessageType(NetIncomingMessageType.DebugMessage);
            config.AutoExpandMTU = true;
            config.Port = 14242;
            NetServer Server = new NetServer(config);
            Task.Run(() =>
            {
                Server.Start();
                NetIncomingMessage inc;
                while ((inc = Server.WaitMessage(100)) != null)
                {
                    switch (inc.MessageType)
                    {
                        case NetIncomingMessageType.Error:
                            break;
                        case NetIncomingMessageType.StatusChanged:
                            break;
                        case NetIncomingMessageType.UnconnectedData:
                            break;
                        case NetIncomingMessageType.ConnectionApproval:
                            break;
                        case NetIncomingMessageType.Data:

                            Console.WriteLine(BitConverter.ToInt32(inc.Data));

                            break;
                        case NetIncomingMessageType.Receipt:
                            break;
                        case NetIncomingMessageType.DiscoveryRequest:
                            break;
                        case NetIncomingMessageType.DiscoveryResponse:
                            break;
                        case NetIncomingMessageType.VerboseDebugMessage:
                            break;
                        case NetIncomingMessageType.DebugMessage:
                            break;
                        case NetIncomingMessageType.WarningMessage:
                            break;
                        case NetIncomingMessageType.ErrorMessage:
                            break;
                        case NetIncomingMessageType.NatIntroductionSuccess:
                            break;
                        case NetIncomingMessageType.ConnectionLatencyUpdated:
                            break;
                        default:
                            break;
                    }
                }
            });

            System.Threading.Thread.Sleep(1000);

            NetPeerConfiguration config1 = new NetPeerConfiguration("ImageTransfer");
            config1.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
            config1.EnableMessageType(NetIncomingMessageType.DiscoveryRequest);
            config1.EnableMessageType(NetIncomingMessageType.DebugMessage);
            config1.AutoExpandMTU = true;

            NetClient Client = new NetClient(config1);
            Client.Start();
            var connection = Client.Connect(new IPEndPoint(IPAddress.Loopback, 14242));

            Console.WriteLine(connection.Status);
            while (connection.Status != NetConnectionStatus.Connected)
            {
                Console.WriteLine(connection.Status);
                System.Threading.Thread.Sleep(1000);
            }

            NetOutgoingMessage msg = new NetOutgoingMessage();
            msg.Data = new byte[1024];
            for (int i = 0; i < 10; i++)
            {
                BitConverter.GetBytes(i).AsSpan().CopyTo(msg.Data.AsSpan());
                Client.SendMessage(msg, NetDeliveryMethod.ReliableOrdered);

            }
        }

        static void Test1()
        {
            int packsize = 1024;
            int packlen = 10000;
            int current = 0;
            var sw = new Stopwatch();

            var server = new UdpClient(5000);
            var client = new UdpClient(5001);
            client.Connect(IPAddress.Loopback, 5000);
            server.Connect(IPAddress.Loopback, 5001);


            PseudoTcpSocket.Callbacks cbsLeft = new PseudoTcpSocket.Callbacks();
            PseudoTcpSocket.Callbacks cbsRight = new PseudoTcpSocket.Callbacks();

            PseudoTcpSocket leftSocket = PseudoTcpSocket.Create(0, cbsLeft);
            PseudoTcpSocket rightSocket = PseudoTcpSocket.Create(0, cbsRight);
            Task.Run(() =>
            {
                IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                while (true)
                {
                    var bytes = client.Receive(ref ep);
                    leftSocket.NotifyPacket(bytes, (uint)bytes.Length);
                    leftSocket.NotifyClock();
                }
            });
            Task.Run(() =>
            {
                IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                while (true)
                {
                    var bytes = server.Receive(ref ep);
                    rightSocket.NotifyPacket(bytes, (uint)bytes.Length);
                    rightSocket.NotifyClock();
                }
            });

            cbsLeft.PseudoTcpOpened = (socket, obj) =>
            {
                Console.WriteLine($"client PseudoTcpOpened");
                Task.Run(async () =>
                {
                    sw.Start();
                    var bytes = new byte[packsize];
                    for (int i = 1; i <= packlen; i++)
                    {
                        //uint available_space = PseudoTcpFifo.GetWriteRemaining(leftSocket.priv.sbuf);
                        //while (available_space < packsize)
                        //{
                        //    await Task.Delay(0);
                        //    available_space = PseudoTcpFifo.GetWriteRemaining(leftSocket.priv.sbuf);
                        //}

                        int len = leftSocket.Send(bytes, (uint)bytes.Length);
                        Console.WriteLine($"{i}:send {len}");
                        await Task.Delay(30);
                    }
                });
            };
            cbsLeft.PseudoTcpReadable = (socket, obj) =>
            {
                Console.WriteLine($"client PseudoTcpReadable");
                Task.Run(() =>
                {
                    while (true)
                    {
                        byte[] buf = new byte[1024];
                        int len;

                        do
                        {
                            len = rightSocket.Recv(buf, (uint)buf.Length);

                            if (len < 0)
                                break;

                            if (len == 0)
                            {
                                rightSocket.Close(false);

                                break;
                            }

                            Console.WriteLine("client: Read {0} bytes", len);

                        } while (len > 0);
                    }
                });
            };
            cbsLeft.PseudoTcpWritable = (socket, obj) =>
            {
                Console.WriteLine($"client PseudoTcpWritable");
            };
            cbsLeft.PseudoTcpClosed = (PseudoTcpSocket tcp, uint error, object data) =>
            {
                Console.WriteLine($"client PseudoTcpClosed");
            };
            cbsLeft.WritePacket = (PseudoTcpSocket tcp, byte[] buffer, uint len, object data) =>
            {
                client.Send(buffer, (int)len);
                return PseudoTcpSocket.WriteResult.WR_SUCCESS;
            };

            cbsRight.PseudoTcpOpened = (socket, obj) =>
            {
                Console.WriteLine($"server PseudoTcpOpened");
            };
            cbsRight.PseudoTcpReadable = (socket, obj) =>
            {
                byte[] buf = new byte[8 * 1024];
                int len;
                do
                {
                    len = rightSocket.Recv(buf, (uint)buf.Length);
                    Console.WriteLine($"Recv {len}");
                    if (len < 0)
                        break;

                    if (len == 0)
                    {
                        rightSocket.Close(false);
                        break;
                    }

                    current += len;
                    Console.WriteLine($"current {current}");
                    if (current >= packsize * packlen)
                    {
                        sw.Stop();
                        Console.WriteLine($"结束:{((packsize * packlen) / 1024.0 / 1024.0) / (sw.ElapsedMilliseconds / 1000.0)}MB/s");
                    }


                    //Console.WriteLine("server: Read {0} bytes", len);

                } while (len > 0);
            };
            cbsRight.PseudoTcpWritable = (socket, obj) =>
            {
                Console.WriteLine($"server PseudoTcpWritable");
            };
            cbsRight.PseudoTcpClosed = (PseudoTcpSocket tcp, uint error, object data) =>
            {
                Console.WriteLine($"server PseudoTcpClosed");
            };
            cbsRight.WritePacket = (PseudoTcpSocket tcp, byte[] buffer, uint len, object data) =>
            {
                server.Send(buffer, (int)len);
                return PseudoTcpSocket.WriteResult.WR_SUCCESS;
            };

            leftSocket.NotifyMtu(1496);
            rightSocket.NotifyMtu(1496);
            leftSocket.Connect();

            Task.Run(async () =>
            {
                ulong timeout = 0;
                while (true)
                {
                    if (leftSocket.GetNextClock(ref timeout))
                    {
                        leftSocket.NotifyClock();
                    }
                    if (rightSocket.GetNextClock(ref timeout))
                    {
                        rightSocket.NotifyClock();
                    }
                    await Task.Delay(15);
                }
            });
        }
    }

}