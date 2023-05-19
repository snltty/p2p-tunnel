using common.libs.extends;
using common.server;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace common.forward
{
    public class ForwardTargetCaching : IForwardTargetCaching<ForwardTargetCacheInfo>
    {
        private readonly ConcurrentDictionary<string, ForwardTargetCacheInfo> cacheHost = new();
        private readonly ConcurrentDictionary<ushort, ForwardTargetCacheInfo> cache = new();
        public ForwardTargetCaching()
        {

        }
        public ForwardTargetCacheInfo Get(string host)
        {
            cacheHost.TryGetValue(host, out ForwardTargetCacheInfo cacheInfo);
            return cacheInfo;
        }
        public ForwardTargetCacheInfo Get(string domain, ushort port)
        {
            return Get(JoinHost(domain, port));
        }
        public ForwardTargetCacheInfo Get(ushort port)
        {
            cache.TryGetValue(port, out ForwardTargetCacheInfo cacheInfo);
            return cacheInfo;
        }
        public bool Add(string domain, ushort port, ForwardTargetCacheInfo mdoel)
        {
            return cacheHost.TryAdd(JoinHost(domain, port), mdoel);
        }
        public bool Add(ushort port, ForwardTargetCacheInfo mdoel)
        {
            bool res = cache.TryAdd(port, mdoel);
            return res;
        }
        public void AddOrUpdate(string domain, ushort port, ForwardTargetCacheInfo mdoel)
        {
            cacheHost.AddOrUpdate(JoinHost(domain, port), mdoel, (a, oldValue) => mdoel);
        }
        public void AddOrUpdate(ushort port, ForwardTargetCacheInfo mdoel)
        {
            cache.AddOrUpdate(port, mdoel, (a, oldValue) => mdoel);
        }
        public bool Remove(string domain, ushort port)
        {
            return cacheHost.TryRemove(JoinHost(domain, port), out _);
        }
        public bool Remove(ushort port)
        {

            return cache.TryRemove(port, out _);
        }
        public List<ushort> Remove(ulong id)
        {
            List<ushort> keys = cache.Where(c => c.Value.ConnectionId == id).Select(c => c.Key).ToList();
            foreach (var key in keys)
            {
                cache.TryRemove(key, out _);
            }
            List<string> keys1 = cacheHost.Where(c => c.Value.ConnectionId == id).Select(c => c.Key).ToList();
            foreach (var key in keys1)
            {
                cacheHost.TryRemove(key, out _);
            }
            return keys;
        }
        public bool Contains(string domain, ushort port)
        {
            return cacheHost.ContainsKey(JoinHost(domain, port));
        }
        public bool Contains(ushort port)
        {
            return cache.ContainsKey(port);
        }

        private string JoinHost(string domain, ushort port)
        {
            return port == 80 || port == 443 ? domain : $"{domain}:{port}";
        }
        public void ClearConnection()
        {
            foreach (var item in cacheHost.Values)
            {
                item.Connection = null;
            }
            foreach (var item in cache.Values)
            {
                item.Connection = null;
            }
        }

        public void ClearConnection(ulong id)
        {
            foreach (var item in cacheHost.Values.Where(c => c.ConnectionId == id))
            {
                item.Connection = null;
            }
            foreach (var item in cache.Values.Where(c => c.ConnectionId == id))
            {
                item.Connection = null;
            }
        }
    }

    public sealed class ForwardTargetCacheInfo
    {
        public ulong ConnectionId { get; set; }
        public string Key { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public IConnection Connection { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public Memory<byte> IPAddress { get; set; }
        public ushort Port { get; set; }
    }
}