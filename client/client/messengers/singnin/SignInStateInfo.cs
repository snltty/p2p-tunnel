using common.libs;
using common.server;
using System;
using System.Net;

namespace client.messengers.singnin
{
    /// <summary>
    /// 本地注册状态
    /// </summary>
    public class SignInStateInfo
    {
        /// <summary>
        /// TCP连接对象
        /// </summary>
        public IConnection Connection { get; set; }
        /// <summary>
        /// tcp是否在线
        /// </summary>
        public bool Connected => Connection != null && Connection.Connected;

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
        public Action<bool> OnChange { get; set; } = (param) => { };
        /// <summary>
        /// 当注册开始绑定
        /// </summary>
        public Action<bool> OnBind { get; set; } = (param) => { };

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
                    if (Connection != null)
                        Connection.ConnectId = connectid;
                }
            }
        }

        /// <summary>
        /// 下线
        /// </summary>
        public void Offline()
        {
            bool online = Connected;
            LocalInfo.IsConnecting = false;
            LocalInfo.Connected = false;

            RemoteInfo.Ip = IPAddress.Any;

            ConnectId = 0;
            Connection?.Disponse();
            Connection = null;

            OnBind?.Invoke(false);
            if (online != Connected)
            {
                OnChange?.Invoke(false);
            }

        }
        /// <summary>
        /// 上线
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ip"></param>
        public void Online(ulong id, IPAddress ip)
        {
            LocalInfo.IsConnecting = false;
            LocalInfo.Connected = id > 0;

            RemoteInfo.Ip = ip;
            ConnectId = id;

            OnChange?.Invoke(true);
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
        /// 客户端连接ID
        /// </summary>
        public ulong ConnectId { get; set; }

        /// <summary>
        /// 服务器权限
        /// </summary>
        public uint Access { get; set; }

        /// <summary>
        /// 剩余流量
        /// </summary>
        public long NetFlow { get; set; }
        /// <summary>
        /// 账号结束时间
        /// </summary>
        public DateTime EndTime { get; set; }
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
        /// 本地TCP端口
        /// </summary>
        public ushort Port { get; set; } = 0;

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
        /// 是否已连接服务器
        /// </summary>
        public bool Connected { get; set; } = false;
    }
}
