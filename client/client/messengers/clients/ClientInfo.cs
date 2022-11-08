using common.server.model;
using System;
using System.Text.Json.Serialization;
using common.server;
using System.ComponentModel;
using System.Net;

namespace client.messengers.clients
{
    /// <summary>
    /// 客户端信息
    /// </summary>
    public class ClientInfo
    {
        public bool UdpConnecting { get; private set; } = false;
        public bool TcpConnecting { get; private set; } = false;
        public bool UdpConnected { get => UdpConnection != null && UdpConnection.Connected; }
        public bool TcpConnected { get => TcpConnection != null && TcpConnection.Connected; }

        public string Name { get; init; } = string.Empty;

        private IPAddress ip = IPAddress.Any;
        public IPAddress Ip { get => ip; }

        public ulong Id { get; init; } = 0;
        public bool UsePunchHole { get; init; } = false;

        public bool UseUdp { get; init; } = false;
        public bool UseTcp { get; init; } = false;
        public bool UseRelay { get; init; } = false;
        public int UdpPing => UdpConnection?.RoundTripTime ?? -1;
        public int TcpPing => TcpConnection?.RoundTripTime ?? -1;


        public ClientConnectTypes UdpConnectType { get; private set; } = ClientConnectTypes.P2P;
        public ClientConnectTypes TcpConnectType { get; private set; } = ClientConnectTypes.P2P;

        [JsonIgnore]
        public byte TryReverseValue { get; set; } = 1;
        [JsonIgnore]
        public IConnection TcpConnection { get; set; } = null;
        [JsonIgnore]
        public IConnection UdpConnection { get; set; } = null;
        [JsonIgnore]
        public IConnection OnlineConnection => TcpConnection ?? UdpConnection;

        public void OfflineUdp()
        {
            UdpConnecting = false;
            if (UdpConnection != null && UdpConnection.Relay == false)
            {
                UdpConnection.Disponse();
            }
            UdpConnection = null;
        }
        public void OfflineTcp()
        {
            TcpConnecting = false;
            if (TcpConnection != null && TcpConnection.Relay == false)
            {
                TcpConnection.Disponse();
            }
            TcpConnection = null;
        }
        public void Offline(ServerType serverType)
        {
            if (serverType == ServerType.UDP)
            {
                OfflineUdp();
            }
            else
            {
                //Offline();
                OfflineTcp();
            }
        }

        public void Online(IConnection connection, ClientConnectTypes connectType)
        {
            if (connection.ServerType == ServerType.UDP)
            {
                UdpConnection = connection;
                ip = connection.Address.Address;
                UdpConnectType = connectType;
            }
            else
            {
                TcpConnection = connection;
                ip = connection.Address.Address;
                TcpConnectType = connectType;
            }
            Connecting(false, connection.ServerType);
        }

        public void Connecting(bool val, IConnection connection)
        {
            Connecting(val, connection.ServerType);
        }

        public void Connecting(bool val, ServerType serverType)
        {
            if (serverType == ServerType.UDP)
            {
                UdpConnecting = val;
            }
            else
            {
                TcpConnecting = val;
            }
        }
    }

    [Flags]
    public enum ClientConnectTypes : byte
    {
        [Description("打洞")]
        P2P = 1 << 0,
        [Description("中继")]
        Relay = 1 << 1
    }
}
