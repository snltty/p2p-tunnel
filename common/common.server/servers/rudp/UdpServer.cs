using common.libs;
using LiteNetLib;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace common.server.servers.rudp
{
    public class UdpServer : IUdpServer
    {
        public SimpleSubPushHandler<IConnection> OnPacket { get; } = new SimpleSubPushHandler<IConnection>();
        public SimpleSubPushHandler<IConnection> OnDisconnect { get; } = new SimpleSubPushHandler<IConnection>();
        public Action<IConnection> OnConnected { get; set; } = (IConnection connection) => { };

        Semaphore maxNumberConnectings = new Semaphore(1, 1);
        NumberSpaceDefault maxNumberConnectingNumberSpace = new NumberSpaceDefault();

        private NetManager server;
        private EventBasedNetListener listener;

        public void Start(int port, IPAddress ip = null)
        {
            listener = new EventBasedNetListener();
            server = new NetManager(listener);
            server.NatPunchEnabled = true;
            server.UnsyncedEvents = true;
            server.PingInterval = 20000;
            server.DisconnectTimeout = 60000;
            server.MaxConnectAttempts = 1;
            server.Start(port);

            listener.ConnectionRequestEvent += request =>
            {
                request.Accept();
            };

            listener.PeerConnectedEvent += peer =>
            {
                RudpConnection connection = new RudpConnection(peer, peer.EndPoint);
                peer.Tag = connection;
                OnConnected(connection);
            };
            listener.PeerDisconnectedEvent += (peer, disconnectInfo) =>
            {
                if (peer.Tag is IConnection connection)
                {
                    OnDisconnect.Push(connection);
                    connection.Disponse();
                }
            };
            listener.NetworkErrorEvent += (endPoint, socketError) =>
            {
            };
            listener.NetworkReceiveEvent += (NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod) =>
            {
                try
                {
                    IConnection connection = peer.Tag as IConnection;
                    connection.ReceiveData = reader.RawData.AsMemory(reader.UserDataOffset, reader.UserDataSize);
                    OnPacket.Push(connection);
                }
                catch (Exception)
                {
                }
            };
        }

        public void Stop()
        {
            if (listener != null)
            {
                listener.ClearConnectionRequestEvent();
                listener.ClearPeerConnectedEvent();
                listener.ClearPeerDisconnectedEvent();
                listener.ClearNetworkReceiveEvent();
            }
            if (server != null)
            {
                server.DisconnectAll();
                server.Stop();
            }
            Release();
        }

        public async Task<IConnection> CreateConnection(IPEndPoint address)
        {
            return await Task.Run(async () =>
            {
                maxNumberConnectings.WaitOne();
                maxNumberConnectingNumberSpace.Increment();
                try
                {
                    var peer = server.Connect(address, string.Empty);
                    while (peer.ConnectionState == ConnectionState.Outgoing)
                    {
                        await Task.Delay(10);
                    }
                    if (peer.ConnectionState == ConnectionState.Connected)
                    {
                        return peer.Tag as RudpConnection;
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (Exception)
                {
                    return null;
                }
                finally
                {
                    Release();
                }
            });
        }

        public bool SendUnconnectedMessage(byte[] message, IPEndPoint address)
        {
            return server.SendUnconnectedMessage(message, address);
        }


        private void Release()
        {
            if (maxNumberConnectingNumberSpace.Get() > 0)
            {
                maxNumberConnectingNumberSpace.Decrement();
                maxNumberConnectings.Release();
            }
        }
    }

    class WaitPeer
    {
        public IPEndPoint InternalAddr { get; }
        public IPEndPoint ExternalAddr { get; }
        public DateTime RefreshTime { get; private set; }

        public void Refresh()
        {
            RefreshTime = DateTime.UtcNow;
        }

        public WaitPeer(IPEndPoint internalAddr, IPEndPoint externalAddr)
        {
            Refresh();
            InternalAddr = internalAddr;
            ExternalAddr = externalAddr;
        }
    }
}
