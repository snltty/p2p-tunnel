using common.libs;
using common.libs.extends;
using common.libs.rateLimit;
using common.server;
using common.server.model;
using common.server.servers.rudp;
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
        private readonly Config config;

        public Action<SignInCacheInfo> OnChanged { get; set; } = (param) => { };
        public Action<SignInCacheInfo> OnLine { get; set; } = (param) => { };
        public Action<SignInCacheInfo> OnOffline { get; set; } = (param) => { };
        private readonly WheelTimer<IConnection> wheelTimer = new WheelTimer<IConnection>();

        private readonly IRateLimit<IPAddress> rateLimit;
        private readonly MessengerSender messengerSender;

        public int Count { get => cache.Count; }

        public ClientSignInCaching(Config config, IUdpServer udpServer, ITcpServer tcpServer, MessengerSender messengerSender)
        {
            this.config = config;
            this.messengerSender = messengerSender;
           
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
        public SignInCacheInfo Add(SignInCacheInfo model)
        {
            if (model.ConnectionId == 0)
            {
                do
                {
                    string str = BitConverter.ToUInt64(Guid.NewGuid().ToByteArray()).ToString();
                    if (str.Length > 15)
                    {
                        str = str.Substring(str.Length - 15, 15);
                    }
                    model.ConnectionId = ulong.Parse(str);

                } while (cache.ContainsKey(model.ConnectionId));
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
            if (model.Connection != null)
            {
                model.Connection.ConnectId = model.ConnectionId;
            }
            value.TryAdd(model.Name, model);
            cache.TryAdd(model.ConnectionId, model);
            OnLine?.Invoke(model);
            return model;
        }
        public bool Get(ulong connectionid, out SignInCacheInfo client)
        {
            return cache.TryGetValue(connectionid, out client);
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
        public void Remove(ulong connectionid)
        {
            if (cache.TryRemove(connectionid, out SignInCacheInfo client))
            {
                client.Connection?.Disponse();
                OnChanged?.Invoke(client);
                Notify(client);
                OnOffline?.Invoke(client);

                if (cacheGroups.TryGetValue(client.GroupId, out ConcurrentDictionary<string, SignInCacheInfo> value))
                {
                    value.TryRemove(client.Name, out _);
                    if (value.Count == 0)
                    {
                        cacheGroups.TryRemove(client.GroupId, out _);
                    }
                    foreach (var item in value)
                    {
                        item.Value.RemoveTunnel(connectionid);
                    }
                }
                GC.Collect();
            }
        }
        public bool Notify(ulong connectionid)
        {
            if (Get(connectionid, out SignInCacheInfo client))
            {
                OnChanged?.Invoke(client);
                Notify(client);
            }
            return false;
        }

        private void Notify(SignInCacheInfo cache)
        {
            List<ClientsClientInfo> clients = Get(cache.GroupId).Where(c => c.Connection != null && c.Connection.Connected).OrderBy(c => c.ConnectionId).Select(c => new ClientsClientInfo
            {
                Connection = c.Connection,
                ConnectionId = c.ConnectionId,
                Name = c.Name,
                ClientAccess = c.ClientAccess,
                UserAccess = c.UserAccess
            }).ToList();

            if (clients.Any())
            {
                byte[] bytes = new ClientsInfo
                {
                    Clients = clients.ToArray()
                }.ToBytes();
                foreach (ClientsClientInfo client in clients)
                {
                    messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = client.Connection,
                        Payload = bytes,
                        MessengerId = (ushort)ClientsMessengerIds.Notify
                    }).Wait();
                }
            }
        }
    }
}
