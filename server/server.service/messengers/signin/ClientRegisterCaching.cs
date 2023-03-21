using common.libs;
using common.libs.extends;
using common.libs.rateLimit;
using common.server;
using server.messengers.singnin;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace server.service.messengers.singnin
{
    public sealed class ClientSignInCaching : IClientSignInCaching
    {
        private readonly ConcurrentDictionary<ulong, SignInCacheInfo> cache = new();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, SignInCacheInfo>> cacheGroups = new();
        private NumberSpace idNs = new NumberSpace(0);
        private readonly Config config;

        public SimpleSubPushHandler<SignInCacheInfo> OnChanged { get; } = new SimpleSubPushHandler<SignInCacheInfo>();
        public SimpleSubPushHandler<SignInCacheInfo> OnOffline { get; } = new SimpleSubPushHandler<SignInCacheInfo>();
        private readonly WheelTimer<IConnection> wheelTimer = new WheelTimer<IConnection>();

        private readonly IRateLimit<IPAddress> rateLimit;

        public int Count { get => cache.Count; }

        public ClientSignInCaching(Config config, IUdpServer udpServer, ITcpServer tcpServer)
        {
            this.config = config;

            if (config.ConnectLimit > 0)
            {
                rateLimit = new TokenBucketRatelimit<IPAddress>(config.ConnectLimit);
            }

            tcpServer.OnDisconnect += Disconnected;
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
        public ulong Add(SignInCacheInfo model)
        {
            if (model.Id == 0)
            {
                model.Id = idNs.Increment();
            }
            if (string.IsNullOrWhiteSpace(model.GroupId))
            {
                model.GroupId = Guid.NewGuid().ToString().Md5();
            }
            if (cacheGroups.TryGetValue(model.GroupId, out ConcurrentDictionary<string, SignInCacheInfo> value) == false)
            {
                value = new ConcurrentDictionary<string, SignInCacheInfo>();
                cacheGroups.TryAdd(model.GroupId, value);
            }
            value.TryAdd(model.Name, model);
            cache.TryAdd(model.Id, model);
            return model.Id;
        }
        public bool Get(ulong id, out SignInCacheInfo client)
        {
            return cache.TryGetValue(id, out client);
        }
        public bool Get(string groupid, string name, out SignInCacheInfo client)
        {
            client = null;
            if (cacheGroups.TryGetValue(groupid, out ConcurrentDictionary<string, SignInCacheInfo> value))
            {
                return value.TryGetValue(name, out client);
            }
            return false;
        }
        public IEnumerable<SignInCacheInfo> Get(string groupid)
        {
            if (cacheGroups.TryGetValue(groupid, out ConcurrentDictionary<string, SignInCacheInfo> value))
            {
                return value.Values;
            }
            return Enumerable.Empty<SignInCacheInfo>();
        }
        public List<SignInCacheInfo> Get()
        {
            return cache.Values.ToList();
        }
        public void Remove(ulong id)
        {
            if (cache.TryRemove(id, out SignInCacheInfo client))
            {
                client.Connection?.Disponse();
                OnChanged.Push(client);
                OnOffline.Push(client);

                if (cacheGroups.TryGetValue(client.GroupId, out ConcurrentDictionary<string, SignInCacheInfo> value))
                {
                    value.TryRemove(client.Name, out _);
                    if (value.Count == 0)
                    {
                        cacheGroups.TryRemove(client.GroupId, out _);
                    }
                    foreach (var item in value)
                    {
                        item.Value.RemoveTunnel(id);
                    }
                }
                GC.Collect();
            }
        }
        public bool Notify(IConnection connection)
        {
            if (Get(connection.ConnectId, out SignInCacheInfo client))
            {
                OnChanged.Push(client);
            }
            return false;
        }
    }
}
