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

        public long LastTime { get; }
        public void UpdateTime(long time);
        public bool IsTimeout(long time, int timeout);
        public bool IsNeedHeart(long time, int timeout);

        public long SendBytes { get; set; }
        public long ReceiveBytes { get; set; }
        public ValueTask<bool> Send(ReadOnlyMemory<byte> data);
        public ValueTask<bool> Send(byte[] data, int length);

        public void Disponse();

        public IConnection Clone();

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

        public MessageRequestWrap ReceiveRequestWrap { get; private set; } = new MessageRequestWrap();
        public MessageResponseWrap ReceiveResponseWrap { get; private set; } = new MessageResponseWrap();
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

        public long LastTime { get; private set; } = DateTimeHelper.GetTimeStamp();
        public void UpdateTime(long time) => LastTime = time;
        public bool IsTimeout(long time, int timeout) => (time - LastTime > timeout);
        public bool IsNeedHeart(long time, int timeout)
        {
            return (time - LastTime > (timeout / 4));
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
    }

    public class UdpConnection : Connection
    {
        public UdpConnection(UdpClient udpcRecv, IPEndPoint address)
        {
            UdpcRecv = udpcRecv;
            Address = address;
        }

        public override bool Connected => UdpcRecv != null;

        public UdpClient UdpcRecv { get; private set; }
        public override ServerType ServerType => ServerType.UDP;

        public override async ValueTask<bool> Send(ReadOnlyMemory<byte> data)
        {
            if (Connected)
            {
                try
                {
#if NET5_0
                    await UdpcRecv.SendAsync(data.ToArray(), data.Length, Address).ConfigureAwait(false);
#else
                    await UdpcRecv.SendAsync(data, Address).ConfigureAwait(false);
#endif
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
            UdpcRecv = null;
        }

        public override IConnection Clone()
        {
            UdpConnection clone = new UdpConnection(UdpcRecv, Address);
            clone.EncodeEnable(Crypto);
            return clone;
        }


    }
    public class RudpConnection : Connection
    {
        public RudpConnection(NetPeer peer, IPEndPoint address)
        {
            NetPeer = peer;
            Address = address;
        }

        public override bool Connected => NetPeer != null;

        public NetPeer NetPeer { get; private set; }
        public override ServerType ServerType => ServerType.UDP;

        public override async ValueTask<bool> Send(ReadOnlyMemory<byte> data)
        {
            return await Send(data.ToArray(), data.Length);
        }
        public override ValueTask<bool> Send(byte[] data, int length)
        {
            if (Connected)
            {
                try
                {
                    lock (this)
                    {
                        int index = 0;
                        while (index < 100 && NetPeer.GetPacketsCountInReliableQueue(0, true) > 100)
                        {
                            System.Threading.Thread.Sleep(1);
                            index++;
                        }
                        NetPeer.Send(data, 0, length, DeliveryMethod.ReliableOrdered);
                        SendBytes += data.Length;
                        return new ValueTask<bool>(true);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.DebugError(ex);
                }
            }
            return new ValueTask<bool>(false);
        }

        public override void Disponse()
        {
            base.Disponse();
            if (NetPeer != null)
            {
                NetPeer.Disconnect();
                NetPeer = null;
            }
        }

        public override IConnection Clone()
        {
            RudpConnection clone = new RudpConnection(NetPeer, Address);
            clone.EncodeEnable(Crypto);
            return clone;
        }
    }

    public class TcpConnection : Connection
    {
        public TcpConnection(Socket tcpSocket)
        {
            TcpSocket = tcpSocket;
            Address = (TcpSocket.RemoteEndPoint as IPEndPoint);
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
            return clone;
        }

    }
}
