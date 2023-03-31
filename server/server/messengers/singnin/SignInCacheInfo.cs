using common.server;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Text.Json.Serialization;

namespace server.messengers.singnin
{
    public sealed class SignInCacheInfo
    {
        [JsonIgnore]
        public IConnection Connection { get; set; }
        /// <summary>
        /// 连接id
        /// </summary>
        public ulong ConnectionId { get; set; }
        
        /// <summary>
        /// 连接短id
        /// </summary>
        public byte ShortId { get; set; }
        /// <summary>
        /// 客户端名
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 分组编号，即使是同一账号，不同分组亦不可见
        /// </summary>
        public string GroupId { get; set; }
        /// <summary>
        /// 额外参数
        /// </summary>
        public Dictionary<string, string> Args { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 客户端本地的ip列表
        /// </summary>
        [JsonIgnore]
        public IPAddress[] LocalIps { get; set; }
        /// <summary>
        /// 客户端连接的本地端口
        /// </summary>
        public int LocalPort { get; set; }
       
        /// <summary>
        /// 客户端自己的权限，客户端开放了哪些权限
        /// </summary>
        public uint ClientAccess { get; set; }

        /// <summary>
        /// 客户端在服务器的权限
        /// </summary>
        public uint UserAccess { get; set; }

        private ConcurrentDictionary<ulong, TunnelCacheInfo> tunnels = new ConcurrentDictionary<ulong, TunnelCacheInfo>();
        public void AddTunnel(TunnelCacheInfo model)
        {
            tunnels.AddOrUpdate(model.SourceId, model, (a, b) => model);
        }
        public void RemoveTunnel(ulong sourceId)
        {
            tunnels.TryRemove(sourceId, out _);
        }
        public bool GetTunnel(ulong sourceId, out TunnelCacheInfo model)
        {
            return tunnels.TryGetValue(sourceId, out model);
        }
    }

    public sealed class TunnelCacheInfo
    {
        public int Port { get; set; }
        public int LocalPort { get; set; }
        public ulong SourceId { get; set; }
    }
}
