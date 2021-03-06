using MessagePack;
using common.server.model;
using System;
using System.Text.Json.Serialization;
using common.server;
using System.ComponentModel;

namespace client.messengers.clients
{
    /// <summary>
    /// 客户端信息
    /// </summary>
    [Serializable, MessagePackObject]
    public class ClientInfo
    {
        [Key(1)]
        public bool UdpConnecting { get; set; } = false;
        [Key(2)]
        public bool TcpConnecting { get; set; } = false;
        [Key(3)]
        public bool UdpConnected { get => UdpConnection != null && UdpConnection.Connected; }
        [Key(4)]
        public bool TcpConnected { get => TcpConnection != null && TcpConnection.Connected; }

        [Key(5)]
        public string Name { get; set; } = string.Empty;
        [Key(6)]
        public string Mac { get; set; } = string.Empty;
        [Key(7)]
        public string Ip { get; set; } = string.Empty;

        [Key(8)]
        public ulong Id { get; set; } = 0;


        [Key(9)]
        public ClientConnectTypes UdpConnectType { get; set; } = ClientConnectTypes.P2P;

        [Key(10)]
        public ClientConnectTypes TcpConnectType { get; set; } = ClientConnectTypes.P2P;

        [JsonIgnore, IgnoreMember]
        public IConnection TcpConnection { get; set; } = null;
        [JsonIgnore, IgnoreMember]
        public IConnection UdpConnection { get; set; } = null;

        public void Offline()
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
                Offline();
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
                Ip = connection.Address.Address.ToString();
                UdpConnectType = connectType;
            }
            else
            {
                TcpConnection = connection;
                Ip = connection.Address.Address.ToString();
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
