﻿using common.server;
using common.server.model;
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
        public byte ShortId { get; set; }
        public string Name { get; set; }
        public string GroupId { get; set; }
        [JsonIgnore]
        public IPAddress[] LocalIps { get; set; }
        public uint ClientAccess { get; set; } = 0;

        public void UpdateConnection(IConnection connection)
        {
            connection.ConnectId = Id;
            if (connection.ServerType == ServerType.TCP)
            {
                TcpConnection = connection;
            }
            else
            {
                UdpConnection = connection;
            }
        }

        private ConcurrentDictionary<ulong, TunnelRegisterCacheInfo> tunnels = new ConcurrentDictionary<ulong, TunnelRegisterCacheInfo>();
        public void AddTunnel(TunnelRegisterCacheInfo model)
        {
            tunnels.AddOrUpdate(model.TunnelName, model, (a, b) => model);
        }
        public void RemoveTunnel(ulong tunnameName)
        {
            tunnels.TryRemove(tunnameName, out _);
        }
        public bool TunnelExists(ulong tunnelName)
        {
            return tunnels.ContainsKey(tunnelName);
        }
        public bool GetTunnel(ulong tunnelName, out TunnelRegisterCacheInfo model)
        {
            return tunnels.TryGetValue(tunnelName, out model);
        }
    }

    public class TunnelRegisterCacheInfo
    {
        public ulong TunnelName { get; set; } = 0;
        public int Port { get; set; } = 0;
        public int LocalPort { get; set; } = 0;
        public ServerType Servertype { get; set; }
        public bool IsDefault { get; set; } = false;
    }
}
