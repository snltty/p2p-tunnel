using common.server;
using common.server.model;
using System.Collections.Concurrent;
using System.Net;
using System.Text.Json.Serialization;

namespace server.messengers.register
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class RegisterCacheInfo
    {
        /// <summary>
        /// 
        /// </summary>
        [JsonIgnore]
        public IConnection TcpConnection { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        [JsonIgnore]
        public IConnection UdpConnection { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        [JsonIgnore]
        public IConnection OnLineConnection => TcpConnection ?? UdpConnection;
        /// <summary>
        /// 
        /// </summary>
        public ulong Id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public byte ShortId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string GroupId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [JsonIgnore]
        public IPAddress[] LocalIps { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public uint ClientAccess { get; set; } = 0;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        public void AddTunnel(TunnelRegisterCacheInfo model)
        {
            tunnels.AddOrUpdate(model.TunnelName, model, (a, b) => model);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tunnameName"></param>
        public void RemoveTunnel(ulong tunnameName)
        {
            tunnels.TryRemove(tunnameName, out _);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tunnelName"></param>
        /// <returns></returns>
        public bool TunnelExists(ulong tunnelName)
        {
            return tunnels.ContainsKey(tunnelName);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tunnelName"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        public bool GetTunnel(ulong tunnelName, out TunnelRegisterCacheInfo model)
        {
            return tunnels.TryGetValue(tunnelName, out model);
        }
    }
    /// <summary>
    /// 
    /// </summary>
    public class TunnelRegisterCacheInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public ulong TunnelName { get; set; } = 0;
        /// <summary>
        /// 
        /// </summary>
        public int Port { get; set; } = 0;
        /// <summary>
        /// 
        /// </summary>
        public int LocalPort { get; set; } = 0;
        /// <summary>
        /// 
        /// </summary>
        public ServerType Servertype { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool IsDefault { get; set; } = false;
    }
}
