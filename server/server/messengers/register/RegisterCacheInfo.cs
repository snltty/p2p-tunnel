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
            tunnels.AddOrUpdate(model.SourceId, model, (a, b) => model);
        }
        public void RemoveTunnel(ulong sourceId)
        {
            tunnels.TryRemove(sourceId, out _);
        }
        public bool GetTunnel(ulong sourceId, out TunnelRegisterCacheInfo model)
        {
            return tunnels.TryGetValue(sourceId, out model);
        }
    }

    public class TunnelRegisterCacheInfo
    {
        public int Port { get; set; } = 0;
        public int LocalPort { get; set; } = 0;
        public ulong SourceId { get; set; }
    }
}
