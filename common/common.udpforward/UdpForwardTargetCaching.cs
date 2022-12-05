using common.server;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace common.udpforward
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class UdpForwardTargetCaching : IUdpForwardTargetCaching<UdpForwardTargetCacheInfo>
    {
        private readonly ConcurrentDictionary<ushort, UdpForwardTargetCacheInfo> cache = new();
        /// <summary>
        /// 
        /// </summary>
        public UdpForwardTargetCaching()
        {

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public UdpForwardTargetCacheInfo Get(ushort port)
        {
            cache.TryGetValue(port, out UdpForwardTargetCacheInfo cacheInfo);
            return cacheInfo;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        /// <param name="mdoel"></param>
        /// <returns></returns>
        public bool Add(ushort port, UdpForwardTargetCacheInfo mdoel)
        {
            bool res = cache.TryAdd(port, mdoel);
            return res;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public bool Remove(ushort port)
        {
            return cache.TryRemove(port, out _);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetName"></param>
        /// <returns></returns>
        public IEnumerable<ushort> Remove(string targetName)
        {
            var keys = cache.Where(c => c.Value.Name == targetName).Select(c => c.Key);
            foreach (var key in keys)
            {
                cache.TryRemove(key, out _);
            }
            return keys;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public IEnumerable<ushort> Remove(ulong id)
        {
            var keys = cache.Where(c => c.Value.Id == id).Select(c => c.Key);
            foreach (var key in keys)
            {
                cache.TryRemove(key, out _);
            }
            return keys;
        }
        /// <summary>
        /// 
        /// </summary>
        public void ClearConnection()
        {
            foreach (var item in cache.Values)
            {
                item.Connection = null;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        public void ClearConnection(string name)
        {
            foreach (var item in cache.Values.Where(c => c.Name == name))
            {
                item.Connection = null;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        public void ClearConnection(ulong id)
        {
            foreach (var item in cache.Values.Where(c => c.Id == id))
            {
                item.Connection = null;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class UdpForwardTargetCacheInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public ulong Id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public IConnection Connection { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Memory<byte> Endpoint { get; set; }
    }
}