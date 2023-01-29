using ENet;
using System.Net;
using System.Threading.Channels;

namespace enet
{
    internal class Program
    {
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
                server.Create(address, 400);

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
                                Console.WriteLine("Client connected - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
                                break;

                            case EventType.Disconnect:
                                Console.WriteLine("Client disconnected - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
                                break;

                            case EventType.Timeout:
                                Console.WriteLine("Client timeout - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
                                break;

                            case EventType.Receive:
                                Console.WriteLine("Packet received from - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP + ", Channel ID: " + netEvent.ChannelID + ", Data length: " + netEvent.Packet.Length);
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
                    System.Threading.Thread.Sleep(2000);
                    using Host client = new Host();

                    Address address = new Address();
                    address.SetIP(IPAddress.Loopback.ToString());
                    address.Port = 5000;

                    Address address1 = new Address();
                    address1.Port = 5001;
                    client.Create(address1, 200);
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
                                        //  Console.WriteLine("Client connected to server");
                                        break;

                                    case EventType.Disconnect:
                                        // Console.WriteLine("Client disconnected from server");
                                        break;

                                    case EventType.Timeout:
                                        // Console.WriteLine("Client connection timeout");
                                        break;

                                    case EventType.Receive:
                                        // Console.WriteLine("Packet received from server - Channel ID: " + netEvent.ChannelID + ", Data length: " + netEvent.Packet.Length);
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
                        Packet packet = default(Packet);
                        for (int i = 0; i < 10; i++)
                        {
                            packet.Create(BitConverter.GetBytes(i), PacketFlags.Reliable);
                            peer.Send(0, ref packet);
                        }
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