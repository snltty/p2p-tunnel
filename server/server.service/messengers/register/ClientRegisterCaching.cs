using common.libs;
using common.libs.extends;
using common.libs.rateLimit;
using common.server;
using server.messengers.register;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace server.service.messengers.register
{
    public class ClientRegisterCaching : IClientRegisterCaching
    {
        private readonly IEnumerable<byte> shortIds = Enumerable.Range(1, 255).Select(c => (byte)c);
        private readonly ConcurrentDictionary<ulong, RegisterCacheInfo> cache = new();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<byte, RegisterCacheInfo>> cacheGroups = new();
        private NumberSpace idNs = new NumberSpace(0);
        private readonly Config config;
        private readonly object lockObject = new object();

        public SimpleSubPushHandler<RegisterCacheInfo> OnChanged { get; } = new SimpleSubPushHandler<RegisterCacheInfo>();
        public SimpleSubPushHandler<RegisterCacheInfo> OnOffline { get; } = new SimpleSubPushHandler<RegisterCacheInfo>();
        private readonly WheelTimer<IConnection> wheelTimer = new WheelTimer<IConnection>();

        private readonly IRateLimit<IPAddress> rateLimit;

        public int Count { get => cache.Count; }

        public ClientRegisterCaching(Config config, IUdpServer udpServer, ITcpServer tcpServer)
        {
            this.config = config;

            if (config.ConnectLimit > 0)
            {
                rateLimit = new TokenBucketRatelimit<IPAddress>(config.ConnectLimit);
            }

            tcpServer.OnDisconnect.Sub(Disconnected);
            udpServer.OnDisconnect.Sub(Disconnected);
            tcpServer.OnConnected = AddConnectedTimeout;
            udpServer.OnConnected = AddConnectedTimeout;
        }

        private void AddConnectedTimeout(IConnection connection)
        {
            if (config.ConnectLimit > 0 && rateLimit.Try(connection.Address.Address, 1) == 0)
            {
                connection.Disponse();
                return;
            }

            wheelTimer.NewTimeout(new WheelTimerTimeoutTask<IConnection>
            {
                Callback = ConnectedTimeout,
                State = connection
            }, config.RegisterTimeout, false);
        }
        private void ConnectedTimeout(WheelTimerTimeout<IConnection> timeout)
        {
            IConnection connection = timeout.Task.State;
            if (connection != null && connection.ConnectId == 0)
            {
                connection.Disponse();
            }
        }
        private void Disconnected(IConnection connection)
        {
            if (connection.ConnectId > 0)
            {
                Remove(connection.ConnectId);
            }
        }

        public ulong Add(RegisterCacheInfo model)
        {
            if (model.Id == 0)
            {
                model.Id = idNs.Increment();
            }
            if (cacheGroups.TryGetValue(model.GroupId, out ConcurrentDictionary<byte, RegisterCacheInfo> value))
            {
                value.TryAdd(model.ShortId, model);
            }
            cache.TryAdd(model.Id, model);
            return model.Id;
        }

        public bool Get(ulong id, out RegisterCacheInfo client)
        {
            return cache.TryGetValue(id, out client);
        }
        public bool Get(string groupid, byte id, out RegisterCacheInfo client)
        {
            client = null;
            if (cacheGroups.TryGetValue(groupid, out ConcurrentDictionary<byte, RegisterCacheInfo> value) == false)
            {
                return cache.TryGetValue(id, out client);
            }
            return false;
        }
        public byte GetShortId(string groupid)
        {
            lock (lockObject)
            {
                if (cacheGroups.TryGetValue(groupid, out ConcurrentDictionary<byte, RegisterCacheInfo> value) == false)
                {
                    value = new ConcurrentDictionary<byte, RegisterCacheInfo>();
                    cacheGroups.TryAdd(groupid, value);
                }
                if (value.Count == shortIds.Count())
                {
                    return 0;
                }
                return shortIds.Except(value.Keys).ElementAt(0);
            }
        }

        public IEnumerable<RegisterCacheInfo> GetBySameGroup(string groupid)
        {
            if (cacheGroups.TryGetValue(groupid, out ConcurrentDictionary<byte, RegisterCacheInfo> value))
            {
                return value.Values;
            }
            return Enumerable.Empty<RegisterCacheInfo>();
        }
        public List<RegisterCacheInfo> GetAll()
        {
            return cache.Values.ToList();
        }

        public void Remove(ulong id)
        {
            if (cache.TryRemove(id, out RegisterCacheInfo client))
            {
                client.UdpConnection?.Disponse();
                client.TcpConnection?.Disponse();
                OnChanged.Push(client);
                OnOffline.Push(client);

                if (cacheGroups.TryGetValue(client.GroupId, out ConcurrentDictionary<byte, RegisterCacheInfo> value))
                {
                    value.TryRemove(client.ShortId, out _);
                    if (value.Count == 0)
                    {
                        cacheGroups.TryRemove(client.GroupId, out _);
                    }
                }
            }
        }

        public bool Notify(IConnection connection)
        {
            if (Get(connection.ConnectId, out RegisterCacheInfo client))
            {
                OnChanged.Push(client);
            }
            return false;
        }
    }
}
