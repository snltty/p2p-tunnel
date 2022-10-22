using common.libs;
using common.server.model;
using LiteNetLib;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace common.server.servers.rudp
{
    public class UdpServer : IUdpServer
    {
        public Func<IConnection, Task> OnPacket { get; set; } = async (connection) => { await Task.CompletedTask; };
        public SimpleSubPushHandler<IConnection> OnDisconnect { get; private set; } = new SimpleSubPushHandler<IConnection>();
        public Action<IConnection> OnConnected { get; set; } = (IConnection connection) => { };

        Semaphore maxNumberConnectings = new Semaphore(1, 1);
        NumberSpaceDefault maxNumberConnectingNumberSpace = new NumberSpaceDefault();

        private NetManager server;
        private EventBasedNetListener listener;

        public void Start(int port, IPAddress ip = null)
        {
            Start(port, ip, 20000);
        }
        public void Start(int port, IPAddress ip = null, int timeout = 20000)
        {
            listener = new EventBasedNetListener();
            server = new NetManager(listener);
            server.ReconnectDelay = Math.Max(timeout / 5, 5000);
            server.UnsyncedEvents = true;
            server.PingInterval = Math.Max(timeout / 5, 5000);
            server.DisconnectTimeout = timeout;
            server.MaxConnectAttempts = 10;
            //server.AutoRecycle = true;
            server.ReuseAddress = true;
            server.Start(port);

            listener.ConnectionRequestEvent += request =>
            {
                request.Accept();
            };

            listener.PeerConnectedEvent += peer =>
            {
                RudpConnection connection = new RudpConnection(peer, peer.EndPoint)
                {
                    ReceiveRequestWrap = new MessageRequestWrap(),
                    ReceiveResponseWrap = new MessageResponseWrap()
                };
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
                    OnPacket(connection).Wait();
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
        public void Disponse()
        {
            Stop();
            OnPacket = null;
            OnDisconnect = null;
            OnConnected = null;
            maxNumberConnectings.Dispose();
            maxNumberConnectingNumberSpace = null;
        }

        public async Task InputData(IConnection connection)
        {
            if (OnPacket != null)
                await OnPacket(connection);
        }

        public async Task<IConnection> CreateConnection(IPEndPoint address)
        {
            maxNumberConnectings.WaitOne();
            maxNumberConnectingNumberSpace.Increment();
            try
            {
                var peer = server.Connect(address, string.Empty);
                int index = 50;
                while (peer.ConnectionState == ConnectionState.Outgoing && index > 0)
                {
                    await Task.Delay(15);
                    index--;
                }
                if (peer.ConnectionState == ConnectionState.Connected)
                {
                    return peer.Tag as RudpConnection;
                }
                return null;
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                Release();
            }
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
}
