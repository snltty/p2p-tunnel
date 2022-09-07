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
        public bool UdpConnecting { get; set; } = false;
        public bool TcpConnecting { get; set; } = false;
        public bool UdpConnected { get => UdpConnection != null && UdpConnection.Connected; }
        public bool TcpConnected { get => TcpConnection != null && TcpConnection.Connected; }

        public string Name { get; set; } = string.Empty;
        public string Mac { get; set; } = string.Empty;
        public IPAddress Ip { get; set; } = IPAddress.Any;
        public ulong Id { get; set; } = 0;
        public bool Udp { get; set; } = false;
        public bool Tcp { get; set; } = false;

        public ClientConnectTypes UdpConnectType { get; set; } = ClientConnectTypes.P2P;
        public ClientConnectTypes TcpConnectType { get; set; } = ClientConnectTypes.P2P;

        [JsonIgnore]
        public IConnection TcpConnection { get; set; } = null;
        [JsonIgnore]
        public IConnection UdpConnection { get; set; } = null;
        [JsonIgnore]
        public IConnection OnlineConnection => TcpConnection ?? UdpConnection;

        public void OfflineUdp()
        {
            UdpConnecting = false;
            if (UdpConnection != null)
            {
                UdpConnection.Disponse();
            }
            UdpConnection = null;
        }
        public void OfflineTcp()
        {
            TcpConnecting = false;
            if (TcpConnection != null)
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
                Ip = connection.Address.Address;
                UdpConnectType = connectType;
            }
            else
            {
                TcpConnection = connection;
                Ip = connection.Address.Address;
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
