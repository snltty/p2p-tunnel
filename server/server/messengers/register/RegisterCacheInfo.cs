using common.libs;
using common.server;
using common.server.model;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Text.Json.Serialization;

namespace server.messengers.register
{
    public class RegisterCacheInfo
    {
        [JsonIgnore]
        public IConnection TcpConnection { get; private set; }
        [JsonIgnore]
        public IConnection UdpConnection { get; private set; }

        [JsonIgnore]
        public IConnection OnLineConnection => TcpConnection ?? UdpConnection;

        public ulong Id { get; set; }
        public string Name { get; set; }
        public string GroupId { get; set; }

        /// <summary>
        /// loopback 、LAN ip
        /// </summary>
        [JsonIgnore]
        public IPAddress[] LocalIps { get; set; }
        [JsonIgnore]
        public string Mac { get; set; }

        public void UpdateUdpInfo(IConnection connection)
        {
            UdpConnection = connection;
            UdpConnection.ConnectId = Id;
        }
        public void UpdateTcpInfo(IConnection connection)
        {
            TcpConnection = connection;
            TcpConnection.ConnectId = Id;
        }

        private ConcurrentDictionary<string, TunnelRegisterCacheInfo> tunnels = new ConcurrentDictionary<string, TunnelRegisterCacheInfo>();
        public void AddTunnel(TunnelRegisterCacheInfo model)
        {
            tunnels.AddOrUpdate(model.TunnelName, model, (a, b) => model);
        }
        public bool TunnelExists(string tunnelName)
        {
            return tunnels.ContainsKey(tunnelName);
        }
        public bool GetTunnel(string tunnelName, out TunnelRegisterCacheInfo model)
        {
            return tunnels.TryGetValue(tunnelName, out model);
        }
    }

    public class TunnelRegisterCacheInfo
    {
        public string TunnelName { get; set; } = string.Empty;
        public int Port { get; set; } = 0;
        public int LocalPort { get; set; } = 0;
        public ServerType Servertype { get; set; }
        public bool IsDefault { get; set; } = false;
    }
}
