using common.libs;
using common.libs.extends;
using common.server.model;
using LiteNetLib;
using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace common.server
{
    /// <summary>
    /// 连接对象
    /// </summary>
    public interface IConnection
    {
        /// <summary>
        /// 连接id
        /// </summary>
        public ulong ConnectId { get; set; }
        /// <summary>
        /// 已连接
        /// </summary>
        public bool Connected { get; }

        /// <summary>
        /// 是否是中继
        /// </summary>
        public bool Relay { get; set; }
        /// <summary>
        /// 中继对象id，通过谁中继的，就是谁的id，直连的跟连接id一样
        /// </summary>
        public Memory<ulong> RelayId { get; set; }
        /// <summary>
        /// 来源客户端，中继时，数据来源可能不是给你发数据的那个
        /// </summary>
        public IConnection FromConnection { get; set; }

        /// <summary>
        /// 加密
        /// </summary>
        public bool EncodeEnabled { get; }
        /// <summary>
        /// 加密对象
        /// </summary>
        public ICrypto Crypto { get; }
        /// <summary>
        /// 启用加密
        /// </summary>
        /// <param name="crypto"></param>
        public void EncodeEnable(ICrypto crypto);
        /// <summary>
        /// 移除加密
        /// </summary>
        public void EncodeDisable();

        /// <summary>
        /// 错误
        /// </summary>
        public SocketError SocketError { get; set; }
        /// <summary>
        /// 地址
        /// </summary>
        public IPEndPoint Address { get; }
        /// <summary>
        /// 连接类型
        /// </summary>
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

        public byte[] ResponseData { get; set; }

        /// <summary>
        /// 已发送字节
        /// </summary>
        public long SendBytes { get; set; }
        /// <summary>
        /// 已接收字节
        /// </summary>
        public long ReceiveBytes { get; set; }
        /// <summary>
        /// rtt
        /// </summary>
        public int RoundTripTime { get; set; }

        /// <summary>
        /// 发送
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public ValueTask<bool> Send(ReadOnlyMemory<byte> data);
        /// <summary>
        /// 发送
        /// </summary>
        /// <param name="data"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public ValueTask<bool> Send(byte[] data, int length);

        /// <summary>
        /// 销毁
        /// </summary>
        public void Disponse();

        /// <summary>
        /// 克隆，主要用于中继
        /// </summary>
        /// <returns></returns>
        public IConnection Clone();

        public void WriteResponse(string str)
        {
            var span = str.GetUTF16Bytes();
            ResponseData = ArrayPool<byte>.Shared.Rent(span.Length + 4);
            str.Length.ToBytes(ResponseData);
            span.CopyTo(ResponseData.AsSpan(4));
        }
        public void WriteResponse(ushort[] nums)
        {
            ResponseData = ArrayPool<byte>.Shared.Rent(nums.Length * 2);
            nums.ToBytes(ResponseData);
        }
        public void Return()
        {
            if (ResponseData != null && ResponseData.Length > 0)
            {
                ArrayPool<byte>.Shared.Return(ResponseData);
            }
            ResponseData = null;
        }

    }

    /// <summary>
    /// 
    /// </summary>
    public abstract class Connection : IConnection
    {
        private ulong connectId = 0;
        /// <summary>
        /// 连接id
        /// </summary>
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
        /// <summary>
        /// 已连接
        /// </summary>
        public virtual bool Connected => false;

        /// <summary>
        /// 已中继
        /// </summary>
        public bool Relay { get; set; } = false;
        /// <summary>
        /// 中继路线
        /// </summary>
        public Memory<ulong> RelayId { get; set; }
        /// <summary>
        /// 来源连接
        /// </summary>
        public IConnection FromConnection { get; set; }

        /// <summary>
        /// 启用加密
        /// </summary>
        public bool EncodeEnabled => Crypto != null;
        /// <summary>
        /// 加密类
        /// </summary>
        public ICrypto Crypto { get; private set; }
        /// <summary>
        /// 启用加密
        /// </summary>
        /// <param name="crypto"></param>
        public void EncodeEnable(ICrypto crypto)
        {
            Crypto = crypto;
        }
        /// <summary>
        /// 移除加密
        /// </summary>
        public void EncodeDisable()
        {
            Crypto = null;
        }

        /// <summary>
        /// 错误
        /// </summary>
        public SocketError SocketError { get; set; } = SocketError.Success;

        /// <summary>
        /// 地址
        /// </summary>
        public IPEndPoint Address { get; protected set; }
        /// <summary>
        /// 连接类型
        /// </summary>
        public virtual ServerType ServerType => ServerType.UDP;

        /// <summary>
        /// 接收请求数据
        /// </summary>
        public MessageRequestWrap ReceiveRequestWrap { get; set; }
        /// <summary>
        /// 接收回执数据
        /// </summary>
        public MessageResponseWrap ReceiveResponseWrap { get; set; }


        private Memory<byte> receiveData;
        /// <summary>
        /// 接收数据
        /// </summary>
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
        public byte[] ResponseData { get; set; }

        /// <summary>
        /// 已发送字节
        /// </summary>
        public long SendBytes { get; set; }
        /// <summary>
        /// 已接收字节
        /// </summary>
        public long ReceiveBytes { get; set; }
        /// <summary>
        /// rtt
        /// </summary>
        public virtual int RoundTripTime { get; set; }

        /// <summary>
        /// 发送
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public abstract ValueTask<bool> Send(ReadOnlyMemory<byte> data);
        /// <summary>
        /// 发送
        /// </summary>
        /// <param name="data"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public abstract ValueTask<bool> Send(byte[] data, int length);

        /// <summary>
        /// 销毁
        /// </summary>
        public virtual void Disponse()
        {
            ReceiveRequestWrap = null;
            ReceiveResponseWrap = null;
        }

        /// <summary>
        /// 克隆
        /// </summary>
        /// <returns></returns>
        public abstract IConnection Clone();
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class RudpConnection : Connection
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="peer"></param>
        /// <param name="address"></param>
        public RudpConnection(NetPeer peer, IPEndPoint address)
        {
            NetPeer = peer;

            if (address.Address.AddressFamily == AddressFamily.InterNetworkV6 && address.Address.IsIPv4MappedToIPv6)
            {
                address = new IPEndPoint(new IPAddress(address.Address.GetAddressBytes()[^4..]), address.Port);
            }

            Address = address;
        }

        /// <summary>
        /// 已连接
        /// </summary>
        public override bool Connected => NetPeer != null;
        /// <summary>
        /// 连接对象
        /// </summary>
        public NetPeer NetPeer { get; private set; }
        /// <summary>
        /// 连接类型
        /// </summary>
        public override ServerType ServerType => ServerType.UDP;
        /// <summary>
        /// rtt
        /// </summary>
        public override int RoundTripTime { get; set; }

        /// <summary>
        /// 发送
        /// </summary>
        /// <param name="data"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public override async ValueTask<bool> Send(byte[] data, int length)
        {
            return await Send(data.AsMemory(0, length));
        }


        SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
        /// <summary>
        /// 发送
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 销毁
        /// </summary>
        public override void Disponse()
        {
            if (Relay == false)
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
        }

        /// <summary>
        /// 克隆
        /// </summary>
        /// <returns></returns>
        public override IConnection Clone()
        {
            RudpConnection clone = new RudpConnection(NetPeer, Address);
            clone.EncodeEnable(Crypto);
            clone.ReceiveRequestWrap = ReceiveRequestWrap;
            clone.ReceiveResponseWrap = ReceiveResponseWrap;
            return clone;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class TcpConnection : Connection
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tcpSocket"></param>
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

        private bool error;
        /// <summary>
        /// 已连接
        /// </summary>
        public override bool Connected => TcpSocket != null && TcpSocket.Connected && error == false;

        /// <summary>
        /// socket
        /// </summary>
        public Socket TcpSocket { get; private set; }
        /// <summary>
        /// 连接类型
        /// </summary>
        public override ServerType ServerType => ServerType.TCP;
        /// <summary>
        /// rtt
        /// </summary>
        public override int RoundTripTime { get; set; }


        SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
        /// <summary>
        /// 发送
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public override async ValueTask<bool> Send(ReadOnlyMemory<byte> data)
        {
            if (Connected)
            {
                try
                {
                    await semaphore.WaitAsync();
                    int length = 0;
                    do
                    {
                        int len = await TcpSocket.SendAsync(data[length..], SocketFlags.None).ConfigureAwait(false);
                        if (len <= 0)
                        {
                            error = true;
                            return false;
                        }
                        length += len;
                    } while (length < data.Length);

                    SendBytes += length;
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
        /// <summary>
        /// 发送
        /// </summary>
        /// <param name="data"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public override async ValueTask<bool> Send(byte[] data, int length)
        {
            return await Send(data.AsMemory(0, length));
        }
        /// <summary>
        /// 销毁
        /// </summary>
        public override void Disponse()
        {
            if (Relay == false)
            {
                base.Disponse();
                if (TcpSocket != null)
                {
                    TcpSocket.SafeClose();
                    TcpSocket.Dispose();
                }
            }
        }
        /// <summary>
        /// 克隆
        /// </summary>
        /// <returns></returns>
        public override IConnection Clone()
        {
            TcpConnection clone = new TcpConnection(TcpSocket);
            clone.EncodeEnable(Crypto);
            clone.ReceiveRequestWrap = ReceiveRequestWrap;
            clone.ReceiveResponseWrap = ReceiveResponseWrap;
            return clone;
        }
    }
}
