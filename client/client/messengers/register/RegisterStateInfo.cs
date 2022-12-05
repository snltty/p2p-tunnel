using common.libs;
using common.server;
using System;
using System.Net;

namespace client.messengers.register
{
    /// <summary>
    /// 本地注册状态
    /// </summary>
    public class RegisterStateInfo
    {
        /// <summary>
        /// TCP连接对象
        /// </summary>
        public IConnection TcpConnection { get; set; }
        /// <summary>
        /// tcp是否在线
        /// </summary>
        public bool TcpOnline => TcpConnection != null && TcpConnection.Connected;
        /// <summary>
        /// UDP连接对象
        /// </summary>
        public IConnection UdpConnection { get; set; }
        /// <summary>
        /// udp是否在线
        /// </summary>
        public bool UdpOnline => UdpConnection != null && UdpConnection.Connected;
        /// <summary>
        /// 在线连接对象
        /// </summary>
        public IConnection OnlineConnection => TcpConnection ?? UdpConnection;


        /// <summary>
        /// 远程信息
        /// </summary>
        public RemoteInfo RemoteInfo { get; set; } = new RemoteInfo();
        /// <summary>
        /// 本地信息
        /// </summary>
        public LocalInfo LocalInfo { get; set; } = new LocalInfo();

        /// <summary>
        /// 当注册状态发生变化
        /// </summary>
        public SimpleSubPushHandler<bool> OnRegisterStateChange { get; } = new SimpleSubPushHandler<bool>();
        /// <summary>
        /// 当注册开始绑定
        /// </summary>
        public SimpleSubPushHandler<bool> OnRegisterBind { get; } = new SimpleSubPushHandler<bool>();

        private ulong connectid = 0;
        /// <summary>
        /// 连接id
        /// </summary>
        public ulong ConnectId
        {
            get
            {
                return connectid;
            }
            set
            {
                connectid = value;
                RemoteInfo.ConnectId = connectid;

                if (connectid > 0)
                {
                    if (UdpConnection != null)
                        UdpConnection.ConnectId = connectid;
                    if (TcpConnection != null)
                        TcpConnection.ConnectId = connectid;
                }
            }
        }

        /// <summary>
        /// 下线
        /// </summary>
        public void Offline()
        {
            bool online = TcpOnline;

            LocalInfo.IsConnecting = false;
            LocalInfo.UdpConnected = false;
            LocalInfo.TcpConnected = false;

            RemoteInfo.Ip = IPAddress.Any;
            RemoteInfo.UdpPort = 0;
            RemoteInfo.TcpPort = 0;

            ConnectId = 0;
            var tcp = TcpConnection;
            TcpConnection = null;
            if (tcp != null)
            {
                tcp.Disponse();
            }
            var udp = UdpConnection;
            UdpConnection = null;
            if (udp != null)
            {
                udp.Disponse();
            }

            OnRegisterBind.Push(false);
            if (online != TcpOnline)
            {
                OnRegisterStateChange.Push(false);
            }

        }
        /// <summary>
        /// 上线
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ip"></param>
        /// <param name="udpPort"></param>
        /// <param name="tcpPort"></param>
        public void Online(ulong id, IPAddress ip, int udpPort, int tcpPort)
        {
            LocalInfo.IsConnecting = false;
            LocalInfo.UdpConnected = udpPort > 0;
            LocalInfo.TcpConnected = tcpPort > 0;

            RemoteInfo.Ip = ip;
            RemoteInfo.UdpPort = udpPort;
            RemoteInfo.TcpPort = tcpPort;

            ConnectId = id;

            OnRegisterStateChange.Push(true);
        }
    }

    /// <summary>
    /// 远程信息
    /// </summary>
    public class RemoteInfo
    {
        /// <summary>
        /// 客户端在远程的ip
        /// </summary>
        public IPAddress Ip { get; set; } = IPAddress.Any;
        /// <summary>
        /// 客户端在远程的TCP端口
        /// </summary>
        public int UdpPort { get; set; } = 0;
        /// <summary>
        /// tcp端口
        /// </summary>
        public int TcpPort { get; set; } = 0;
        /// <summary>
        /// 客户端连接ID
        /// </summary>
        public ulong ConnectId { get; set; } = 0;

        /// <summary>
        /// 服务器中继是否开启
        /// </summary>
        public bool Relay { get; set; } = false;
    }

    /// <summary>
    /// 本地信息
    /// </summary>
    public class LocalInfo
    {
        /// <summary>
        /// 外网距离
        /// </summary>
        public ushort RouteLevel { get; set; } = 0;

        /// <summary>
        /// 本地UDP端口
        /// </summary>
        public ushort UdpPort { get; set; } = 0;
        /// <summary>
        /// 本地TCP端口
        /// </summary>
        public ushort TcpPort { get; set; } = 0;

        /// <summary>
        /// 本地ip
        /// </summary>
        public IPAddress LocalIp { get; set; } = IPAddress.Any;

        /// <summary>
        /// 本地ipv6
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public IPAddress[] Ipv6s { get; set; } = Array.Empty<IPAddress>();

        /// <summary>
        /// 是否正在连接服务器
        /// </summary>
        public bool IsConnecting { get; set; } = false;
        /// <summary>
        /// UDP是否已连接服务器
        /// </summary>
        public bool UdpConnected { get; set; } = false;
        /// <summary>
        /// TCP是否已连接服务器
        /// </summary>
        public bool TcpConnected { get; set; } = false;
    }
}
