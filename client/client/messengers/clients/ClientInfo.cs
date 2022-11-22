using common.server.model;
using System;
using System.Text.Json.Serialization;
using common.server;

namespace client.messengers.clients
{
    /// <summary>
    /// 客户端信息
    /// </summary>
    public class ClientInfo
    {
        public bool Connecting { get; private set; } = false;
        public bool Connected { get => Connection != null && Connection.Connected; }

        public string Name { get; init; } = string.Empty;
        public ulong Id { get; init; } = 0;
        public bool UsePunchHole { get; init; } = false;
        public bool UseUdp { get; init; } = false;
        public bool UseTcp { get; init; } = false;
        public bool UseRelay { get; init; } = false;
        public int Ping => Connection?.RoundTripTime ?? -1;

        public ServerType ServerType => Connection?.ServerType ?? ServerType.TCPUDP;
        public ClientConnectTypes ConnectType { get; private set; } = ClientConnectTypes.Unknow;

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
        [JsonIgnore]
        public byte TryReverseValue { get; set; } = 0;
        [JsonIgnore]
        public IConnection Connection { get; set; } = null;

        public void Offline()
        {
            Connecting = false;
            if (Connection != null && Connection.Relay == false)
            {
                Connection.Disponse();
            }
            Connection = null;
            ConnectType = ClientConnectTypes.Unknow;
        }
        public void Online(IConnection connection, ClientConnectTypes connectType)
        {
            Connection = connection;
            ConnectType = connectType;
            SetConnecting(false);
        }
        public void SetConnecting(bool val)
        {
            Connecting = val;
        }
    }

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
}
