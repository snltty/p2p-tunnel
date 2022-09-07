using common.server;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace common.udpforward
{
    public class UdpForwardTargetCaching : IUdpForwardTargetCaching<UdpForwardTargetCacheInfo>
    {
        private readonly ConcurrentDictionary<int, UdpForwardTargetCacheInfo> cache = new();

        public UdpForwardTargetCaching()
        {

        }

        public UdpForwardTargetCacheInfo Get(int port)
        {
            cache.TryGetValue(port, out UdpForwardTargetCacheInfo cacheInfo);
            return cacheInfo;
        }
        public bool Add(int port, UdpForwardTargetCacheInfo mdoel)
        {
            bool res = cache.TryAdd(port, mdoel);
            return res;
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
            return keys;
        }

        public void ClearConnection()
        {
            foreach (var item in cache.Values)
            {
                item.Connection = null;
            }
        }
        public void ClearConnection(string name)
        {
            foreach (var item in cache.Values.Where(c => c.Name == name))
            {
                item.Connection = null;
            }
        }
    }

    public class UdpForwardTargetCacheInfo
    {
        public string Name { get; set; }
        public IConnection Connection { get; set; }
        public Memory<byte> Endpoint { get; set; }
        public UdpForwardTunnelTypes TunnelType { get; set; } = UdpForwardTunnelTypes.TCP_FIRST;
    }
}