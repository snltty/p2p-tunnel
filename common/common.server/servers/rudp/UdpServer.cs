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
        public Action<IPEndPoint, Memory<byte>> OnMessage { get; set; } = (endpoint, data) => { };
        public Action<IConnection> OnDisconnect { get; set; } = (IConnection connection) => { };
        public Action<IConnection> OnConnected { get; set; } = (IConnection connection) => { };

        Semaphore maxNumberConnectings = new Semaphore(1, 1);
        BoolSpace maxNumberConnectingNumberSpace = new BoolSpace(false);

        private NetManager server;
        private EventBasedNetListener listener;

        public void Start(int port)
        {
            Start(port, 20000);
        }
        public void Start(int port, int timeout = 20000)
        {
            listener = new EventBasedNetListener();
            server = new NetManager(listener);
            server.UnconnectedMessagesEnabled = true;
            server.UnsyncedEvents = true;
            server.UpdateTime = 5;
            server.PingInterval = Math.Max(timeout / 5, 5000);
            server.DisconnectTimeout = timeout;
            server.MaxConnectAttempts = 10;
            server.WindowSize = 1024;
            server.MtuOverride = 1400;
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
                    OnDisconnect?.Invoke(connection);
                    connection.Disponse();
                }
            };
            listener.NetworkErrorEvent += (endPoint, socketError) =>
            {
                Logger.Instance.Error($"NetworkErrorEvent:{endPoint}-{socketError}");
            };
            listener.NetworkReceiveEvent += (NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod) =>
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
            listener.NetworkReceiveUnconnectedEvent += (IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) =>
            {
                OnMessage?.Invoke(remoteEndPoint, reader.RawData.AsMemory(reader.UserDataOffset, reader.UserDataSize));
            };
        }

        public void Stop()
        {
            if (server != null)
            {
                server.DisconnectAll();
                server.Stop();
            }
            if (listener != null)
            {
                listener.ClearConnectionRequestEvent();
                listener.ClearPeerConnectedEvent();
                listener.ClearPeerDisconnectedEvent();
                listener.ClearNetworkReceiveEvent();
            }
            Release();
        }
        public void Disponse()
        {
            try
            {
                Stop();
                OnPacket = null;
                OnMessage = null;
                OnDisconnect = null;
                OnConnected = null;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
        }
        public async Task InputData(IConnection connection)
        {
            if (OnPacket != null)
                await OnPacket(connection);
        }

        public async Task<IConnection> CreateConnection(IPEndPoint address)
        {
            maxNumberConnectings.WaitOne();
            maxNumberConnectingNumberSpace.Reverse();
            try
            {
                var peer = server.Connect(address, string.Empty);
                int index = 20;
                while (peer.ConnectionState == ConnectionState.Outgoing && index > 0)
                {
                    await Task.Delay(15);
                    index--;
                }
                if (peer.ConnectionState == ConnectionState.Connected)
                {
                    var rudp = peer.Tag as RudpConnection;
                    return rudp;
                }
                return null;
            }
            catch (Exception ex)
            {
                Logger.Instance.DebugError(ex);
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

        public void SetSpeedLimit(int limit)
        {
            if (server != null)
            {
                server.SpeedLimit = limit;
                RudpConnection.tokenBucketRatelimit.ChangeRate(limit);
            }
        }

        private void Release()
        {
            try
            {
                if (maxNumberConnectingNumberSpace != null && maxNumberConnectingNumberSpace.IsDefault == false)
                {
                    maxNumberConnectingNumberSpace.Reset();
                    maxNumberConnectings.Release();
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
        }

    }
}
