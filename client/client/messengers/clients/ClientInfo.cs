using common.server.model;
using System;
using System.Text.Json.Serialization;
using common.server;
using System.Net;
using System.Collections.Generic;

namespace client.messengers.clients
{
    /// <summary>
    /// 客户端信息
    /// </summary>
    public sealed class ClientInfo
    {
        /// <summary>
        /// 连接中
        /// </summary>
        public bool Connecting { get; private set; }
        /// <summary>
        /// 连接对象
        /// </summary>
        [JsonIgnore]
        public IConnection Connection { get; set; }
        /// <summary>
        /// 已连接
        /// </summary>
        public bool Connected { get => Connection != null && Connection.Connected; }
        /// <summary>
        /// ip
        /// </summary>
        public IPEndPoint IPAddress { get => Connection?.Address ?? new IPEndPoint(0, 0); }

        /// <summary>
        /// 名字
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 连接id
        /// </summary>
        public ulong ConnectionId { get; set; }

        /// <summary>
        /// 对方客户端的一些配置权限
        /// </summary>
        public uint ClientAccess { get; set; }
        /// <summary>
        /// 对方客户端在服务器的权限，或者通信功能权限
        /// </summary>
        public uint UserAccess { get; set; }

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
        /// 离线类型
        /// </summary>
        public ClientOfflineTypes OfflineType { get; private set; } = ClientOfflineTypes.Disconnect;

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
        public byte TryReverseValue { get; set; }

        /// <summary>
        /// 通道服务
        /// </summary>
        [JsonIgnore]
        public IServer TunnelServer { get; set; }
        /// <summary>
        /// 通道端口
        /// </summary>
        [JsonIgnore]
        public int TunnelPort { get; set; }


        [JsonIgnore]
        public int OfflineTimes { get; set; }

        [JsonIgnore]
        public int TryTimes { get; set; }


        public bool GetConnect()
        {
            return OfflineType == ClientOfflineTypes.Disconnect && Connecting == false && TryTimes <= 5;
        }

        /// <summary>
        /// 下线
        /// </summary>
        /// <param name="offlineType"></param>
        public void Offline(ClientOfflineTypes offlineType = ClientOfflineTypes.Manual)
        {
            OfflineTimes++;
            Connecting = false;
            ConnectType = ClientConnectTypes.Unknow;
            OfflineType = offlineType;
            Connection?.Disponse();
            Connection = null;

            TunnelServer?.Disponse();
            TunnelServer = null;
        }
        /// <summary>
        /// 上线
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="connectType"></param>
        /// <param name="onlineType"></param>
        public void Online(IConnection connection, ClientConnectTypes connectType)
        {
            OfflineType = ClientOfflineTypes.Disconnect;
            OfflineTimes = 0;
            TryTimes = 0;
            IConnection _connection = Connection;
            Connection = connection;
            ConnectType = connectType;
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
