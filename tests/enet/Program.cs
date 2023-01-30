using ENet;
using System.Diagnostics;
using System.Net;
using System.Threading.Channels;

namespace enet
{
    internal unsafe class Program
    {
        static int packSize = 2 * 1024;
        static int packCount = 10000;
        static Stopwatch sw = new Stopwatch();

        static void Main(string[] args)
        {
            ENet.Library.Initialize();
            Server();
            Client();
            Console.ReadLine();
        }

        static void Server()
        {
            Task.Run(() =>
            {
                Host server = new Host();
                Address address = new Address();
                address.Port = 5000;
                server.Create(address, 400, 255, 1 * 1024 * 1024, 1 * 1024 * 1024);
                server.SetBandwidthThrottle();

                Event netEvent;
                while (true)
                {
                    bool polled = false;
                    while (!polled)
                    {
                        if (server.CheckEvents(out netEvent) <= 0)
                        {
                            if (server.Service(15, out netEvent) <= 0)
                            {
                                break;
                            }
                            polled = true;
                        }
                        switch (netEvent.Type)
                        {
                            case EventType.None:
                                break;

                            case EventType.Connect:
                                Console.WriteLine("Client connected - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP + "，port:" + netEvent.Peer.Port);
                                break;

                            case EventType.Disconnect:
                                Console.WriteLine("Client disconnected - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP + "，port:" + netEvent.Peer.Port);
                                break;

                            case EventType.Timeout:
                                Console.WriteLine("Client timeout - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP + "，port:" + netEvent.Peer.Port);
                                break;

                            case EventType.Receive:
                                {
                                    try
                                    {
                                        int i = BitConverter.ToInt32(new Span<byte>((void*)(netEvent.Packet.Data), netEvent.Packet.Length));

                                        if (i == packCount)
                                        {
                                            sw.Stop();
                                            Console.WriteLine($"{((packSize * packCount / 1024.0 / 1024) / (sw.ElapsedMilliseconds / 1000.0))}MB/s");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex + "");
                                    }
                                }
                                netEvent.Packet.Dispose();
                                break;
                        }
                    }
                }
            });
        }

        static void Client()
        {
            Task.Run(() =>
            {
                try
                {
                    int queue = 0;


                    System.Threading.Thread.Sleep(2000);
                    Host client = new Host();


                    Address address = new Address();
                    address.SetIP(IPAddress.Loopback.ToString());
                    address.Port = 5000;

                    Address address1 = new Address();
                    address1.Port = 5001;
                    client.Create(address1, 400, 255, 1 * 1024 * 1024, 1 * 1024 * 1024);
                    client.SetBandwidthThrottle();
                    Peer peer = client.Connect(address);

                    Event netEvent;
                    Task.Run(() =>
                    {
                        while (true)
                        {
                            bool polled = false;
                            while (!polled)
                            {
                                if (client.CheckEvents(out netEvent) <= 0)
                                {
                                    if (client.Service(15, out netEvent) <= 0)
                                        break;

                                    polled = true;
                                }

                                switch (netEvent.Type)
                                {
                                    case EventType.None:
                                        break;

                                    case EventType.Connect:
                                        Console.WriteLine("Client connected to server");
                                        break;

                                    case EventType.Disconnect:
                                        Console.WriteLine("Client disconnected from server");
                                        break;

                                    case EventType.Timeout:
                                        Console.WriteLine("Client connection timeout");
                                        break;

                                    case EventType.Receive:
                                        {
                                            int i = BitConverter.ToInt32(new Span<byte>((void*)new IntPtr(netEvent.Packet.UserData), netEvent.Packet.Length));
                                            Console.WriteLine(i);
                                        }
                                        netEvent.Packet.Dispose();
                                        break;
                                }
                            }
                        }
                    });

                    while (peer.State == PeerState.Connecting)
                    {
                        Console.WriteLine($"连接:{peer.State}");
                        System.Threading.Thread.Sleep(1000);
                    }


                    Task.Run(() =>
                    {
                        sw.Start();
                        byte[] bytes = new byte[packSize];
                        Packet packet = default(Packet);
                        for (int i = 1; i <= packCount; i++)
                        {
                            BitConverter.GetBytes(i).AsSpan().CopyTo(bytes);
                            packet.Create(bytes, PacketFlags.Reliable);

                            if (peer.Send(0, ref packet) == false)
                            {
                                Console.WriteLine("发送失败");
                            }
                            packet.Dispose();
                        }
                        Console.WriteLine("发送完成====");
                    });
                }
                catch (Exception ex)
                {

                    Console.WriteLine(ex + "");
                }
            });
        }
    }
}