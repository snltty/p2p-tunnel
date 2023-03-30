using common.libs;
using common.libs.extends;
using common.server.model;
using LiteNetLib;
using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
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

        #region 中继
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
        #endregion

        #region 加密
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
        #endregion

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
        /// rtt
        /// </summary>
        public int RoundTripTime { get; set; }

        #region 接收数据
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
        #endregion

        /// <summary>
        /// 发送
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public Task<bool> Send(ReadOnlyMemory<byte> data, bool logger = false);
        /// <summary>
        /// 发送
        /// </summary>
        /// <param name="data"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public Task<bool> Send(byte[] data, int length, bool logger = false);

        /// <summary>
        /// 销毁
        /// </summary>
        public void Disponse();
        /// <summary>
        /// 克隆，主要用于中继
        /// </summary>
        /// <returns></returns>
        public IConnection Clone();



        #region 回复消息相关

        public Memory<byte> ResponseData { get; }
        public void Write(Memory<byte> data);
        public void Write(ulong num);
        public void Write(ushort num);
        public void Write(ushort[] nums);
        /// <summary>
        /// 英文多用这个
        /// </summary>
        /// <param name="str"></param>
        public void WriteUTF8(string str);
        /// <summary>
        /// 中文多用这个
        /// </summary>
        /// <param name="str"></param>
        public void WriteUTF16(string str);
        /// <summary>
        /// 归还池
        /// </summary>
        public void Return();
        #endregion


        public Task WaitOne();
        public void Release();

        /// <summary>
        /// 发送了多少数据
        /// </summary>
        public Action<long> DataSendt { get; set; }
        /// <summary>
        /// 流量限制回调，false被限制，true通行
        /// </summary>
        public Func<bool> NetFlow { get; set; }

        /// <summary>
        /// 引用相等
        /// </summary>
        /// <param name="connection1"></param>
        /// <param name="connection2"></param>
        /// <returns></returns>
        public static bool Equals(IConnection connection1, IConnection connection2)
        {
            if (connection1 == null || connection2 == null)
            {
                return false;
            }
            return ReferenceEquals(connection1, connection2);
        }
        /// <summary>
        /// 引用相等或者地址相等
        /// </summary>
        /// <param name="connection1"></param>
        /// <param name="connection2"></param>
        /// <returns></returns>
        public static bool Equals2(IConnection connection1, IConnection connection2)
        {
            if (connection1 == null || connection2 == null)
            {
                return false;
            }
            return ReferenceEquals(connection1, connection2) || connection1.Address.Equals(connection2.Address);
        }
    }

    public abstract class Connection : IConnection
    {
        public Connection()
        {
        }

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
        /// rtt
        /// </summary>
        public virtual int RoundTripTime { get; set; }


        #region 中继
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
        #endregion

        #region 加密
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
        #endregion

        #region 接收数据
        /// <summary>
        /// 接收请求数据
        /// </summary>
        public MessageRequestWrap ReceiveRequestWrap { get; set; }
        /// <summary>
        /// 接收回执数据
        /// </summary>
        public MessageResponseWrap ReceiveResponseWrap { get; set; }
        /// <summary>
        /// 接收数据
        /// </summary>
        public Memory<byte> ReceiveData { get; set; }
        #endregion

        #region 回复数据
        public Memory<byte> ResponseData { get; private set; }
        private byte[] responseData;
        private int length = 0;

        public void Write(Memory<byte> data)
        {
            ResponseData = data;
        }
        public void Write(ulong num)
        {
            length = 8;
            responseData = ArrayPool<byte>.Shared.Rent(length);
            num.ToBytes(responseData);
            ResponseData = responseData.AsMemory(0, length);
        }
        public void Write(ushort num)
        {
            length = 2;
            responseData = ArrayPool<byte>.Shared.Rent(length);
            num.ToBytes(responseData);
            ResponseData = responseData.AsMemory(0, length);
        }
        public void Write(ushort[] nums)
        {
            length = nums.Length * 2;
            responseData = ArrayPool<byte>.Shared.Rent(length);
            nums.ToBytes(responseData);
            ResponseData = responseData.AsMemory(0, length);
        }
        /// <summary>
        /// 英文多用这个
        /// </summary>
        /// <param name="str"></param>
        public void WriteUTF8(string str)
        {
            var span = str.AsSpan();
            responseData = ArrayPool<byte>.Shared.Rent((span.Length + 1) * 3 + 8);
            var memory = responseData.AsMemory();

            int utf8Length = span.ToUTF8Bytes(memory.Slice(8));
            span.Length.ToBytes(memory);
            utf8Length.ToBytes(memory.Slice(4));
            length = utf8Length + 8;

            ResponseData = responseData.AsMemory(0, length);
        }
        /// <summary>
        /// 中文多用这个
        /// </summary>
        /// <param name="str"></param>
        public void WriteUTF16(string str)
        {
            var span = str.GetUTF16Bytes();
            length = span.Length + 4;
            responseData = ArrayPool<byte>.Shared.Rent(length);
            str.Length.ToBytes(responseData);
            span.CopyTo(responseData.AsSpan(4));

            ResponseData = responseData.AsMemory(0, length);
        }
        /// <summary>
        /// 归还池
        /// </summary>
        public void Return()
        {
            if (length > 0 && ResponseData.Length > 0)
            {
                ArrayPool<byte>.Shared.Return(responseData);
            }
            ResponseData = Helper.EmptyArray;
            responseData = null;
            length = 0;
        }
        #endregion


        /// <summary>
        /// 发送
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public abstract Task<bool> Send(ReadOnlyMemory<byte> data, bool logger = false);
        /// <summary>
        /// 发送
        /// </summary>
        /// <param name="data"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public abstract Task<bool> Send(byte[] data, int length, bool logger = false);

        /// <summary>
        /// 销毁
        /// </summary>
        public virtual void Disponse()
        {
            try
            {
                if (Semaphore != null)
                {
                    if (locked)
                    {
                        locked = false;
                        Semaphore.Release();
                    }
                    Semaphore.Dispose();
                }
                Semaphore = null;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
            //ReceiveRequestWrap = null;
            //ReceiveResponseWrap = null;
        }
        /// <summary>
        /// 克隆
        /// </summary>
        /// <returns></returns>
        public abstract IConnection Clone();


        SemaphoreSlim Semaphore = new SemaphoreSlim(1);
        bool locked = false;
        public virtual async Task WaitOne()
        {
            try
            {
                if (Semaphore != null)
                {
                    locked = true;
                    await Semaphore.WaitAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }

        }
        public virtual void Release()
        {
            try
            {
                if (Semaphore != null)
                {
                    locked = false;
                    Semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
        }


        public Action<long> DataSendt { get; set; } = (length) => { };
        public Func<bool> NetFlow { get; set; } = () => true;
    }

    public sealed class RudpConnection : Connection
    {
        public RudpConnection(NetPeer peer, IPEndPoint address) : base()
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
        public override bool Connected => NetPeer != null && NetPeer.ConnectionState == ConnectionState.Connected;
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


        public static TokenBucketRatelimit tokenBucketRatelimit = new TokenBucketRatelimit(50 * 1024);
        /// <summary>
        /// 发送
        /// </summary>
        /// <param name="data"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public override async Task<bool> Send(byte[] data, int length, bool logger = false)
        {
            return await Send(data.AsMemory(0, length), logger);
        }
        /// <summary>
        /// 发送
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public override async Task<bool> Send(ReadOnlyMemory<byte> data, bool logger = false)
        {
            if (Connected && NetFlow() == true)
            {
                try
                {
                    int index = 0;
                    while (NetPeer.GetPacketsCountInReliableQueue(0, true) > 75)
                    {
                        if (index >= 10000 / 30 || Connected == false)
                        {
                            return false;
                        }
                        NetPeer.Update();
                        await Task.Delay(30);
                        index++;
                    }
                    int len = 0;
                    do
                    {
                        len = tokenBucketRatelimit.Try(data.Length);
                        if (len < data.Length)
                        {
                            await Task.Delay(30);
                        }
                        if (Connected == false) return false;
                    } while (len < data.Length);

                    NetPeer.Send(data, 0, data.Length, DeliveryMethod.ReliableOrdered);
                    NetPeer.Update();
                    DataSendt(data.Length);

                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Instance.DebugError(ex);
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

            }
        }

        /// <summary>
        /// 克隆
        /// </summary>
        /// <returns></returns>
        public override IConnection Clone()
        {
            RudpConnection clone = new RudpConnection(NetPeer, Address);
            //clone.EncodeEnable(Crypto);
            clone.ReceiveRequestWrap = ReceiveRequestWrap;
            clone.ReceiveResponseWrap = ReceiveResponseWrap;
            return clone;
        }
    }

    public sealed class TcpConnection : Connection
    {
        public TcpConnection(Socket tcpSocket) : base()
        {
            TcpSocket = tcpSocket;

            IPEndPoint address = (TcpSocket.RemoteEndPoint as IPEndPoint);
            if (address.Address.AddressFamily == AddressFamily.InterNetworkV6 && address.Address.IsIPv4MappedToIPv6)
            {
                address = new IPEndPoint(new IPAddress(address.Address.GetAddressBytes()[^4..]), address.Port);
            }
            Address = address;
        }

        /// <summary>
        /// 已连接
        /// </summary>
        public override bool Connected => TcpSocket != null && TcpSocket.Connected;

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

        /// <summary>
        /// 发送
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public override async Task<bool> Send(ReadOnlyMemory<byte> data, bool logger = false)
        {
            if (Connected && NetFlow() == true)
            {
                try
                {
                    await TcpSocket.SendAsync(data, SocketFlags.None);
                    DataSendt(data.Length);
                    return true;
                }
                catch (Exception ex)
                {
                    Disponse();
                    Logger.Instance.DebugError(ex);
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
        public override async Task<bool> Send(byte[] data, int length, bool logger = false)
        {
            return await Send(data.AsMemory(0, length), logger);
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
            //clone.EncodeEnable(Crypto);
            clone.ReceiveRequestWrap = ReceiveRequestWrap;
            clone.ReceiveResponseWrap = ReceiveResponseWrap;
            return clone;
        }
    }


    public sealed class TokenBucketRatelimit
    {
        double ticks = 1000.0d * TimeSpan.TicksPerMillisecond;
        TokenBucketRateInfo info;

        public TokenBucketRatelimit(int rate)
        {
            info = new TokenBucketRateInfo { Rate = rate, CurrentRate = 0, Token = rate / ticks, LastTime = GetTime() };
        }

        public void ChangeRate(int rate)
        {
            info.Rate = rate;
            info.Token = rate / ticks;
        }

        public int Try(int num)
        {
            if (info.Rate == 0)
            {
                return num;
            }
            AddToken(info);
            //消耗掉能消耗的
            int canEat = Math.Min(num, (int)info.CurrentRate);
            if (canEat >= num)
            {
                info.CurrentRate -= canEat;
            }
            return canEat;
        }

        private void AddToken(TokenBucketRateInfo info)
        {
            long time = GetTime();
            long times = (time - info.LastTime);

            info.CurrentRate = Math.Min(info.CurrentRate + times * info.Token, info.Rate);

            info.LastTime = time;
        }

        private long GetTime()
        {
            return DateTime.UtcNow.Ticks;
        }

        class TokenBucketRateInfo
        {
            public double Rate { get; set; }
            public double CurrentRate { get; set; }
            public double Token { get; set; }
            public long LastTime { get; set; }

        }
    }
}
