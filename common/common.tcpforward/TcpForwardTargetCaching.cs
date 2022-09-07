using common.server;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace common.tcpforward
{
    public class TcpForwardTargetCaching : ITcpForwardTargetCaching<TcpForwardTargetCacheInfo>
    {
        private readonly ConcurrentDictionary<string, TcpForwardTargetCacheInfo> cacheHost = new();
        private readonly ConcurrentDictionary<int, TcpForwardTargetCacheInfo> cache = new();

        public TcpForwardTargetCaching()
        {

        }

        public TcpForwardTargetCacheInfo Get(string host)
        {
            cacheHost.TryGetValue(host, out TcpForwardTargetCacheInfo cacheInfo);
            return cacheInfo;
        }
        public TcpForwardTargetCacheInfo Get(string domain, int port)
        {
            return Get(JoinHost(domain, port));
        }
        public TcpForwardTargetCacheInfo Get(int port)
        {
            cache.TryGetValue(port, out TcpForwardTargetCacheInfo cacheInfo);
            return cacheInfo;
        }

        public bool Add(string domain, int port, TcpForwardTargetCacheInfo mdoel)
        {
            return cacheHost.TryAdd(JoinHost(domain, port), mdoel);
        }
        public bool Add(int port, TcpForwardTargetCacheInfo mdoel)
        {
            bool res = cache.TryAdd(port, mdoel);
            return res;
        }

        public void AddOrUpdate(string domain, int port, TcpForwardTargetCacheInfo mdoel)
        {
            cacheHost.AddOrUpdate(JoinHost(domain, port), mdoel, (a, oldValue) => mdoel);
        }
        public void AddOrUpdate(int port, TcpForwardTargetCacheInfo mdoel)
        {
            cache.AddOrUpdate(port, mdoel, (a, oldValue) => mdoel);
        }

        public bool Remove(string domain, int port)
        {
            return cacheHost.TryRemove(JoinHost(domain, port), out _);
        }
        public bool Remove(int port)
        {
            return cache.TryRemove(port, out _);
        }
        public IEnumerable<int> Remove(string targetName)
        {
            var keys = cache.Where(c => c.Value.Name == targetName).Select(c => c.Key);
            foreach (var key in keys)
            {
                cache.TryRemove(key, out _);
            }
            var keys1 = cacheHost.Where(c => c.Value.Name == targetName).Select(c => c.Key);
            foreach (var key in keys1)
            {
                cacheHost.TryRemove(key, out _);
            }
            return keys;
        }

        public bool Contains(string domain, int port)
        {
            return cacheHost.ContainsKey(JoinHost(domain, port));
        }
        public bool Contains(int port)
        {
            return cache.ContainsKey(port);
        }

        private string JoinHost(string domain, int port)
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
        public void ClearConnection(string name)
        {
            foreach (var item in cacheHost.Values.Where(c => c.Name == name))
            {
                item.Connection = null;
            }
            foreach (var item in cache.Values.Where(c => c.Name == name))
            {
                item.Connection = null;
            }
        }
    }

    public class TcpForwardTargetCacheInfo
    {
        public string Name { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public IConnection Connection { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public Memory<byte> Endpoint { get; set; }
        public TcpForwardTunnelTypes TunnelType { get; set; } = TcpForwardTunnelTypes.TCP_FIRST;
        public TcpForwardTypes ForwardType { get; set; } = TcpForwardTypes.FORWARD;
    }
}