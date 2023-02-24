using Lidgren.Network;
using System;

namespace lidgren
{
    internal class Program
    {
        static int currentLength = 0;
        static int oldLength = 0;
        static void Main(string[] args)
        {
            string type = Console.ReadLine();
            switch (type)
            {
                case "c":
                    Client();
                    break;
                case "s":
                    Server();
                    Speed();
                    break;
                default:
                    break;
            }

            Console.ReadLine();
        }

        static void Speed()
        {
            Task.Run(() =>
            {
                Thread.Sleep(1000);
                while (true)
                {
                    Console.WriteLine($"{(currentLength - oldLength) * 1.0 / 1024 / 1024}/MB/s");
                    oldLength = currentLength;

                    Thread.Sleep(1000);
                }
            });
        }

        static void Client()
        {
            Task.Run(() =>
            {
                NetClient s_client = null;
                try
                {
                    NetPeerConfiguration config = new NetPeerConfiguration("speedtest");
                    config.AutoExpandMTU = true;
                    config.Port = 14243;
                    config.AutoFlushSendQueue = true;
                    s_client = new NetClient(config);
                    s_client.Start();
                    s_client.Connect(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 14242));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex + "");
                }

                Task.Run(() =>
                {
                    while (true)
                    {
                        NetIncomingMessage im;
                        while ((im = s_client.ReadMessage()) != null)
                        {
                            s_client.Recycle(im);
                        }

                        Thread.Sleep(0);
                    }
                });

                Task.Run(() =>
                {
                  
                    byte[] tmp = new byte[1024];
                    MWCRandom.Instance.NextBytes(tmp);
                 
                    try
                    {
                        while (true)
                        {

                            while (s_client.ServerConnection != null && s_client.ServerConnection.Status == NetConnectionStatus.Connected)
                            {
                                s_client.ServerConnection.GetSendQueueInfo(NetDeliveryMethod.ReliableOrdered, 0, out int windowSize, out int freeWindowSlots);
                                if (windowSize == 0)
                                {
                                    freeWindowSlots = 1;
                                }

                                while (freeWindowSlots > -windowSize)
                                {

                                    NetOutgoingMessage om = s_client.CreateMessage(1024);
                                    om.Write(tmp);
                                    NetSendResult res = s_client.SendMessage(om, NetDeliveryMethod.ReliableOrdered, 0);
                                    if (NetDeliveryMethod.ReliableOrdered != NetDeliveryMethod.Unreliable && NetDeliveryMethod.ReliableOrdered != NetDeliveryMethod.UnreliableSequenced)
                                    {
                                        if (res != NetSendResult.Queued && res != NetSendResult.Sent)
                                            throw new NetException("Got res " + res);
                                    }

                                    freeWindowSlots--;
                                }

                                System.Threading.Thread.Sleep(0);
                            }
                            System.Threading.Thread.Sleep(1000);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex + "");
                    }

                });
            });
        }

        static void Server()
        {
            Task.Run(() =>
            {
                NetPeerConfiguration config = new NetPeerConfiguration("speedtest");
                config.MaximumConnections = 10;
                config.Port = 14242;
                config.AutoFlushSendQueue = true;
                NetServer s_server = new NetServer(config);
                s_server.Start();

                while (true)
                {
                    NetIncomingMessage im;
                    while ((im = s_server.ReadMessage()) != null)
                    {
                        // handle incoming message
                        switch (im.MessageType)
                        {
                            case NetIncomingMessageType.DebugMessage:
                            case NetIncomingMessageType.ErrorMessage:
                            case NetIncomingMessageType.WarningMessage:
                            case NetIncomingMessageType.VerboseDebugMessage:
                                break;
                            case NetIncomingMessageType.StatusChanged:
                                break;
                            case NetIncomingMessageType.Data:
                                currentLength += im.LengthBytes;
                                Thread.Sleep(50);
                                break;
                            default:
                                break;
                        }
                        s_server.Recycle(im);
                    }

                    Thread.Sleep(0);
                }
            });
        }
    }
}