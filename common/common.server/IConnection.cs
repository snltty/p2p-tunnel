using common.libs;
using common.libs.extends;
using common.server.model;
using LiteNetLib;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace common.server
{
    public interface IConnection
    {
        /// <summary>
        /// 连接id
        /// </summary>
        public ulong ConnectId { get; set; }
        public bool Connected { get; }

        /// <summary>
        /// 是否是中继
        /// </summary>
        public bool Relay { get; set; }
        /// <summary>
        /// 中继对象id，通过谁中继的，就是谁的id，直连的跟连接id一样
        /// </summary>
        public ulong[] RelayId { get; set; }
        /// <summary>
        /// 来源客户端，中继时，数据来源可能不是给你发数据的那个
        /// </summary>
        public IConnection FromConnection { get; set; }

        public bool EncodeEnabled { get; }
        public ICrypto Crypto { get; }
        public void EncodeEnable(ICrypto crypto);
        public void EncodeDisable();

        public SocketError SocketError { get; set; }
        public IPEndPoint Address { get; }
        public ServerType ServerType { get; }

        /// <summary>
        /// 请求数据包装对象
        /// </summary>
        public MessageRequestWrap ReceiveRequestWrap { get; }
        /// <summary>
        /// 回复数据包装对象
        /// </summary>
        public MessageResponseWrap ReceiveResponseWrap { get; }
        /// <summary>
        /// 接收到的原始数据
        /// </summary>
        public Memory<byte> ReceiveData { get; set; }

        public long SendBytes { get; set; }
        public long ReceiveBytes { get; set; }
        public int RoundTripTime { get; set; }

        public ValueTask<bool> Send(ReadOnlyMemory<byte> data);
        public ValueTask<bool> Send(byte[] data, int length);

        public void Disponse();

        public IConnection Clone();
        public IConnection CloneFull();

    }

    public abstract class Connection : IConnection
    {
        private ulong connectId = 0;
        public ulong ConnectId
        {
            get
            {
                return connectId;
            }
            set
            {
                connectId = value;
            }
        }
        public virtual bool Connected => false;

        public bool Relay { get; set; } = false;
        public ulong[] RelayId { get; set; } = Helper.EmptyUlongArray;
        public IConnection FromConnection { get; set; }

        public bool EncodeEnabled => Crypto != null;
        public ICrypto Crypto { get; private set; }
        public void EncodeEnable(ICrypto crypto)
        {
            Crypto = crypto;
        }
        public void EncodeDisable()
        {
            Crypto = null;
        }

        public SocketError SocketError { get; set; } = SocketError.Success;

        public IPEndPoint Address { get; protected set; }
        public virtual ServerType ServerType => ServerType.UDP;

        public MessageRequestWrap ReceiveRequestWrap { get; set; }
        public MessageResponseWrap ReceiveResponseWrap { get; set; }

        private Memory<byte> receiveData = Helper.EmptyArray;
        public Memory<byte> ReceiveData
        {
            get
            {
                return receiveData;
            }
            set
            {
                receiveData = value;
                ReceiveBytes += receiveData.Length;
            }
        }

        public long SendBytes { get; set; } = 0;
        public long ReceiveBytes { get; set; } = 0;
        public virtual int RoundTripTime { get; set; } = 0;

        public abstract ValueTask<bool> Send(ReadOnlyMemory<byte> data);
        public abstract ValueTask<bool> Send(byte[] data, int length);

        public virtual void Disponse()
        {
            ReceiveRequestWrap = null;
            ReceiveResponseWrap = null;
        }

        public abstract IConnection Clone();
        public abstract IConnection CloneFull();
    }

    public sealed class RudpConnection : Connection
    {
        public RudpConnection(NetPeer peer, IPEndPoint address)
        {
            NetPeer = peer;

            if (address.Address.AddressFamily == AddressFamily.InterNetworkV6 && address.Address.IsIPv4MappedToIPv6)
            {
                address = new IPEndPoint(new IPAddress(address.Address.GetAddressBytes()[^4..]), address.Port);
            }

            Address = address;
        }

        public override bool Connected => NetPeer != null;

        public NetPeer NetPeer { get; private set; }
        public override ServerType ServerType => ServerType.UDP;
        public override int RoundTripTime { get; set; } = 0;

        public override async ValueTask<bool> Send(byte[] data, int length)
        {
            return await Send(data.AsMemory(0, length));
        }


        SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
        public override async ValueTask<bool> Send(ReadOnlyMemory<byte> data)
        {
            if (Connected)
            {
                try
                {
                    await semaphore.WaitAsync();
                    int index = 0;
                    while (index < 100 && NetPeer.GetPacketsCountInReliableQueue(0, true) > 75)
                    {
                        index++;
                        await Task.Delay(1);
                    }

                    NetPeer.Send(data, 0, data.Length, DeliveryMethod.ReliableOrdered);
                    NetPeer.Update();
                    SendBytes += data.Length;

                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Instance.DebugError(ex);
                }
                finally
                {
                    try
                    {
                        semaphore.Release();
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            return false;
        }

        public override void Disponse()
        {
            base.Disponse();
            if (NetPeer != null)
            {
                if (NetPeer.ConnectionState == ConnectionState.Connected)
                {
                    NetPeer.Disconnect();
                }
                NetPeer = null;
            }
            semaphore.Dispose();
        }

        public override IConnection Clone()
        {
            RudpConnection clone = new RudpConnection(NetPeer, Address);
            clone.EncodeEnable(Crypto);
            clone.ReceiveRequestWrap = ReceiveRequestWrap;
            clone.ReceiveResponseWrap = ReceiveResponseWrap;
            return clone;
        }
        public override IConnection CloneFull()
        {
            RudpConnection clone = new RudpConnection(NetPeer, Address);
            clone.EncodeEnable(Crypto);
            clone.ReceiveRequestWrap = ReceiveRequestWrap;
            clone.ReceiveResponseWrap = ReceiveResponseWrap;
            clone.ConnectId = ConnectId;
            clone.Relay = Relay;
            clone.SendBytes = SendBytes;
            clone.ReceiveBytes = ReceiveBytes;
            return clone;
        }
    }

    public sealed class TcpConnection : Connection
    {
        public TcpConnection(Socket tcpSocket)
        {
            TcpSocket = tcpSocket;

            IPEndPoint address = (TcpSocket.RemoteEndPoint as IPEndPoint);
            if (address.Address.AddressFamily == AddressFamily.InterNetworkV6 && address.Address.IsIPv4MappedToIPv6)
            {
                address = new IPEndPoint(new IPAddress(address.Address.GetAddressBytes()[^4..]), address.Port);
            }
            Address = address;
        }

        public override bool Connected => TcpSocket != null && TcpSocket.Connected;

        public Socket TcpSocket { get; private set; }
        public override ServerType ServerType => ServerType.TCP;
        public override int RoundTripTime { get; set; } = 0;

        public override async ValueTask<bool> Send(ReadOnlyMemory<byte> data)
        {
            if (Connected)
            {
                try
                {
                    await TcpSocket.SendAsync(data, SocketFlags.None).ConfigureAwait(false);
                    SendBytes += data.Length;
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Instance.DebugError(ex);
                }
            }
            return false;
        }
        public override ValueTask<bool> Send(byte[] data, int length)
        {
            return Send(data.AsMemory(0, length));
        }

        public override void Disponse()
        {
            base.Disponse();
            if (TcpSocket != null)
            {
                TcpSocket.SafeClose();
                TcpSocket.Dispose();
            }
        }

        public override IConnection Clone()
        {
            TcpConnection clone = new TcpConnection(TcpSocket);
            clone.EncodeEnable(Crypto);
            clone.ReceiveRequestWrap = ReceiveRequestWrap;
            clone.ReceiveResponseWrap = ReceiveResponseWrap;
            return clone;
        }

        public override IConnection CloneFull()
        {
            TcpConnection clone = new TcpConnection(TcpSocket);
            clone.EncodeEnable(Crypto);
            clone.ReceiveRequestWrap = ReceiveRequestWrap;
            clone.ReceiveResponseWrap = ReceiveResponseWrap;
            clone.ConnectId = ConnectId;
            clone.Relay = Relay;
            clone.SendBytes = SendBytes;
            clone.ReceiveBytes = ReceiveBytes;
            return clone;
        }

    }
}
