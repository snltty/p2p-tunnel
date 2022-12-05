using common.server.model;
using System;
using System.Text.Json.Serialization;
using common.server;
using System.Net;

namespace client.messengers.clients
{
    /// <summary>
    /// 客户端信息
    /// </summary>
    public class ClientInfo
    {
        /// <summary>
        /// 连接中
        /// </summary>
        public bool Connecting { get; private set; } = false;
        /// <summary>
        /// 已连接
        /// </summary>
        public bool Connected { get => Connection != null && Connection.Connected; }
        /// <summary>
        /// ip
        /// </summary>
        public IPAddress IPAddress { get => Connection?.Address.Address ?? IPAddress.Any; }

        /// <summary>
        /// 名字
        /// </summary>
        public string Name { get; init; } = string.Empty;
        /// <summary>
        /// 连接id
        /// </summary>
        public ulong Id { get; init; } = 0;
        /// <summary>
        /// 自动打洞
        /// </summary>
        public bool UsePunchHole { get; init; } = false;
        /// <summary>
        /// 用udp
        /// </summary>
        public bool UseUdp { get; init; } = false;
        /// <summary>
        /// 用tco
        /// </summary>
        public bool UseTcp { get; init; } = false;
        /// <summary>
        /// 中继节点
        /// </summary>
        public bool UseRelay { get; init; } = false;
        /// <summary>
        /// ping值
        /// </summary>
        public int Ping => Connection?.RoundTripTime ?? -1;

        /// <summary>
        /// 注册类型
        /// </summary>
        public ServerType ServerType => Connection?.ServerType ?? ServerType.TCPUDP;
        /// <summary>
        /// 连接类型
        /// </summary>
        public ClientConnectTypes ConnectType { get; private set; } = ClientConnectTypes.Unknow;
        /// <summary>
        /// 上线类型
        /// </summary>
        public ClientOnlineTypes OnlineType { get; private set; } = ClientOnlineTypes.Unknow;
        /// <summary>
        /// 离线类型
        /// </summary>
        public ClientOfflineTypes OfflineType { get; private set; } = ClientOfflineTypes.Unknow;

        /// <summary>
        /// tcp状态位
        /// </summary>
        [JsonIgnore]
        public const byte TryReverseTcpBit = 0b00000010;
        /// <summary>
        /// udp状态位
        /// </summary>
        [JsonIgnore]
        public const byte TryReverseUdpBit = 0b00000001;
        /// <summary>
        /// tcp+udp状态位
        /// </summary>
        [JsonIgnore]
        public const byte TryReverseTcpUdpBit = TryReverseTcpBit | TryReverseUdpBit;
        /// <summary>
        /// 默认状态
        /// </summary>
        [JsonIgnore]
        public const byte TryReverseDefault = 0;
        /// <summary>
        /// 打洞重试状态缓存
        /// </summary>
        [JsonIgnore]
        public byte TryReverseValue { get; set; } = 0;
        /// <summary>
        /// 连接对象
        /// </summary>
        [JsonIgnore]
        public IConnection Connection { get; set; } = null;

        /// <summary>
        /// 下线
        /// </summary>
        /// <param name="offlineType"></param>
        public void Offline(ClientOfflineTypes offlineType = ClientOfflineTypes.Manual)
        {
            Connecting = false;
            ConnectType = ClientConnectTypes.Unknow;
            OfflineType = offlineType;
            if (Connection != null && Connection.Relay == false)
            {
                Connection.Disponse();
            }
            Connection = null;
        }
        /// <summary>
        /// 上线
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="connectType"></param>
        /// <param name="onlineType"></param>
        public void Online(IConnection connection, ClientConnectTypes connectType, ClientOnlineTypes onlineType)
        {
            Connection = connection;
            ConnectType = connectType;
            OnlineType = onlineType;
            Connecting = false;
        }
        /// <summary>
        /// 设置连接中状态
        /// </summary>
        /// <param name="val"></param>
        public void SetConnecting(bool val)
        {
            Connecting = val;
        }
    }

    /// <summary>
    /// 连接类型
    /// </summary>
    [Flags]
    public enum ClientConnectTypes : byte
    {
        /// <summary>
        /// 未连接
        /// </summary>
        Unknow = 0,
        /// <summary>
        /// 打洞
        /// </summary>
        P2P = 1,
        /// <summary>
        /// 节点中继
        /// </summary>
        RelayNode = 2,
        /// <summary>
        /// 服务器中继
        /// </summary>
        RelayServer = 4
    }

    /// <summary>
    /// 上线类型
    /// </summary>
    [Flags]
    public enum ClientOnlineTypes : byte
    {
        /// <summary>
        /// 未知的
        /// </summary>
        Unknow = 0,
        /// <summary>
        /// 主动的
        /// </summary>
        Active = 1,
        /// <summary>
        /// 被动的
        /// </summary>
        Passive = 2,
    }

    /// <summary>
    /// 下线类型
    /// </summary>
    [Flags]
    public enum ClientOfflineTypes : byte
    {
        /// <summary>
        /// 未知的
        /// </summary>
        Unknow = 0,
        /// <summary>
        /// 掉线
        /// </summary>
        Disconnect = 1,
        /// <summary>
        /// 主动的
        /// </summary>
        Manual = 2,
    }
}
