using common.libs;
using common.server.model;
using LiteNetLib;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace common.server.servers.rudp
{
    /// <summary>
    /// 
    /// </summary>
    public class UdpServer : IUdpServer
    {
        /// <summary>
        /// 
        /// </summary>
        public Func<IConnection, Task> OnPacket { get; set; } = async (connection) => { await Task.CompletedTask; };
        /// <summary>
        /// 
        /// </summary>
        public SimpleSubPushHandler<IConnection> OnDisconnect { get; private set; } = new SimpleSubPushHandler<IConnection>();
        /// <summary>
        /// 
        /// </summary>
        public Action<IConnection> OnConnected { get; set; } = (IConnection connection) => { };

        Semaphore maxNumberConnectings = new Semaphore(1, 1);
        BoolSpace maxNumberConnectingNumberSpace = new BoolSpace(false);

        private NetManager server;
        private EventBasedNetListener listener;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        public void Start(int port)
        {
            Start(port, 20000);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        /// <param name="timeout"></param>
        public void Start(int port, int timeout = 20000)
        {
            listener = new EventBasedNetListener();
            server = new NetManager(listener);
            server.ReconnectDelay = Math.Max(timeout / 5, 5000);
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
                    OnDisconnect.Push(connection);
                    connection.Disponse();
                }
            };
            listener.NetworkErrorEvent += (endPoint, socketError) =>
            {
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
        }
        /// <summary>
        /// 
        /// </summary>
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
        /// <summary>
        /// 
        /// </summary>
        public void Disponse()
        {
            Stop();
            OnPacket = null;
            OnDisconnect = null;
            OnConnected = null;
            maxNumberConnectings.Dispose();
            maxNumberConnectingNumberSpace = null;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public async Task InputData(IConnection connection)
        {
            if (OnPacket != null)
                await OnPacket(connection);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
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
                    return peer.Tag as RudpConnection;
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="address"></param>
        /// <returns></returns>
        public bool SendUnconnectedMessage(byte[] message, IPEndPoint address)
        {
            return server.SendUnconnectedMessage(message, address);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="limit"></param>
        public void SetSpeedLimit(int limit)
        {
            if (server != null)
            {
                server.SpeedLimit = limit;
            }
        }

        private void Release()
        {
            if (maxNumberConnectingNumberSpace != null && maxNumberConnectingNumberSpace.IsDefault == false)
            {
                maxNumberConnectingNumberSpace.Reset();
                maxNumberConnectings.Release();
            }
        }
    }
}
