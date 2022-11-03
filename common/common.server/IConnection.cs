using common.libs;
using common.libs.extends;
using common.server.model;
using LiteNetLib;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace common.server
{
    public interface IConnection
    {
        public ulong ConnectId { get; set; }
        public bool Connected { get; }

        public bool Relay { get; set; }
        public IConnection FromConnection { get; set; }

        public bool EncodeEnabled { get; }
        public ICrypto Crypto { get; }
        public void EncodeEnable(ICrypto crypto);
        public void EncodeDisable();

        public SocketError SocketError { get; set; }

        public IPEndPoint Address { get; }

        public ServerType ServerType { get; }

        public MessageRequestWrap ReceiveRequestWrap { get; }
        public MessageResponseWrap ReceiveResponseWrap { get; }
        public Memory<byte> ReceiveData { get; set; }

        public long SendBytes { get; set; }
        public long ReceiveBytes { get; set; }
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

    public class RudpConnection : Connection
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

        public override async ValueTask<bool> Send(ReadOnlyMemory<byte> data)
        {
            return await Send(data.ToArray(), data.Length);
        }

        public override async ValueTask<bool> Send(byte[] data, int length)
        {
            if (Connected)
            {
                try
                {
                    int index = 0;
                    while (index < 100 && NetPeer.GetPacketsCountInReliableQueue(0, true) > 75)
                    {
                        index++;
                        await Task.Delay(5);
                    }

                    NetPeer.Send(data, 0, length, DeliveryMethod.ReliableOrdered);
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

    public class TcpConnection : Connection
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
