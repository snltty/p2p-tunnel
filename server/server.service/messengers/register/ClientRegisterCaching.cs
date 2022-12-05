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
    /// <summary>
    /// 
    /// </summary>
    public sealed class ClientRegisterCaching : IClientRegisterCaching
    {
        private readonly ConcurrentDictionary<ulong, RegisterCacheInfo> cache = new();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, RegisterCacheInfo>> cacheGroups = new();
        private NumberSpace idNs = new NumberSpace(0);
        private readonly Config config;

        /// <summary>
        /// 
        /// </summary>
        public SimpleSubPushHandler<RegisterCacheInfo> OnChanged { get; } = new SimpleSubPushHandler<RegisterCacheInfo>();
        /// <summary>
        /// 
        /// </summary>
        public SimpleSubPushHandler<RegisterCacheInfo> OnOffline { get; } = new SimpleSubPushHandler<RegisterCacheInfo>();
        private readonly WheelTimer<IConnection> wheelTimer = new WheelTimer<IConnection>();

        private readonly IRateLimit<IPAddress> rateLimit;

        /// <summary>
        /// 
        /// </summary>
        public int Count { get => cache.Count; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        /// <param name="udpServer"></param>
        /// <param name="tcpServer"></param>
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="client"></param>
        /// <returns></returns>
        public bool Get(ulong id, out RegisterCacheInfo client)
        {
            return cache.TryGetValue(id, out client);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="groupid"></param>
        /// <param name="name"></param>
        /// <param name="client"></param>
        /// <returns></returns>
        public bool Get(string groupid, string name, out RegisterCacheInfo client)
        {
            client = null;
            if (cacheGroups.TryGetValue(groupid, out ConcurrentDictionary<string, RegisterCacheInfo> value))
            {
                return value.TryGetValue(name, out client);
            }
            return false;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="groupid"></param>
        /// <returns></returns>
        public IEnumerable<RegisterCacheInfo> Get(string groupid)
        {
            if (cacheGroups.TryGetValue(groupid, out ConcurrentDictionary<string, RegisterCacheInfo> value))
            {
                return value.Values;
            }
            return Enumerable.Empty<RegisterCacheInfo>();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public List<RegisterCacheInfo> Get()
        {
            return cache.Values.ToList();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        public void Remove(ulong id)
        {
            if (cache.TryRemove(id, out RegisterCacheInfo client))
            {
                client.UdpConnection?.Disponse();
                client.TcpConnection?.Disponse();
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
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
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
