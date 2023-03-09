using common.server;
using common.server.model;
using System.Collections.Concurrent;
using System.Net;
using System.Text.Json.Serialization;

namespace server.messengers.register
{
    public sealed class RegisterCacheInfo
    {
        [JsonIgnore]
        public IConnection Connection { get; private set; }
        public ulong Id { get; set; }
        public byte ShortId { get; set; }
        public string Name { get; set; }
        public string GroupId { get; set; }
        [JsonIgnore]
        public IPAddress[] LocalIps { get; set; }
        public uint ClientAccess { get; set; } = 0;

        public int Port { get; set; } = 0;
        public int LocalPort { get; set; } = 0;

        public void UpdateConnection(IConnection connection)
        {
            connection.ConnectId = Id;
            Connection = connection;
        }

        private ConcurrentDictionary<ulong, TunnelRegisterCacheInfo> tunnels = new ConcurrentDictionary<ulong, TunnelRegisterCacheInfo>();
        public void AddTunnel(TunnelRegisterCacheInfo model)
        {
            tunnels.AddOrUpdate(model.TargetId, model, (a, b) => model);
        }
        public void RemoveTunnel(ulong targetId)
        {
            tunnels.TryRemove(targetId, out _);
        }
        public bool GetTunnel(ulong targetId, out TunnelRegisterCacheInfo model)
        {
            return tunnels.TryGetValue(targetId, out model);
        }
    }

    public class TunnelRegisterCacheInfo
    {
        public int Port { get; set; } = 0;
        public int LocalPort { get; set; } = 0;
        public ulong TargetId { get; set; }
    }
}
