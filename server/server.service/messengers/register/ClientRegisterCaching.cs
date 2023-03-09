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

namespace server.service.messengers.register
{
    public sealed class ClientRegisterCaching : IClientRegisterCaching
    {
        private readonly ConcurrentDictionary<ulong, RegisterCacheInfo> cache = new();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, RegisterCacheInfo>> cacheGroups = new();
        private NumberSpace idNs = new NumberSpace(0);
        private readonly Config config;

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

            tcpServer.OnDisconnect+= Disconnected;
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
            if (string.IsNullOrWhiteSpace(model.GroupId))
            {
                model.GroupId = Guid.NewGuid().ToString().Md5();
            }
            if (cacheGroups.TryGetValue(model.GroupId, out ConcurrentDictionary<string, RegisterCacheInfo> value) == false)
            {
                value = new ConcurrentDictionary<string, RegisterCacheInfo>();
                cacheGroups.TryAdd(model.GroupId, value);
            }
            value.TryAdd(model.Name, model);
            cache.TryAdd(model.Id, model);
            return model.Id;
        }
        public bool Get(ulong id, out RegisterCacheInfo client)
        {
            return cache.TryGetValue(id, out client);
        }
        public bool Get(string groupid, string name, out RegisterCacheInfo client)
        {
            client = null;
            if (cacheGroups.TryGetValue(groupid, out ConcurrentDictionary<string, RegisterCacheInfo> value))
            {
                return value.TryGetValue(name, out client);
            }
            return false;
        }
        public IEnumerable<RegisterCacheInfo> Get(string groupid)
        {
            if (cacheGroups.TryGetValue(groupid, out ConcurrentDictionary<string, RegisterCacheInfo> value))
            {
                return value.Values;
            }
            return Enumerable.Empty<RegisterCacheInfo>();
        }
        public List<RegisterCacheInfo> Get()
        {
            return cache.Values.ToList();
        }
        public void Remove(ulong id)
        {
            if (cache.TryRemove(id, out RegisterCacheInfo client))
            {
                client.Connection?.Disponse();
                OnChanged.Push(client);
                OnOffline.Push(client);

                if (cacheGroups.TryGetValue(client.GroupId, out ConcurrentDictionary<string, RegisterCacheInfo> value))
                {
                    value.TryRemove(client.Name, out _);
                    if (value.Count == 0)
                    {
                        cacheGroups.TryRemove(client.GroupId, out _);
                    }
                }
                GC.Collect();
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
