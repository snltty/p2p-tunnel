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
    public class ClientRegisterCaching : IClientRegisterCaching
    {
        private readonly ConcurrentDictionary<ulong, RegisterCacheInfo> cache = new();
        private NumberSpace idNs = new NumberSpace(0);
        private readonly Config config;

        public SimpleSubPushHandler<RegisterCacheInfo> OnChanged { get; } = new SimpleSubPushHandler<RegisterCacheInfo>();
        public SimpleSubPushHandler<RegisterCacheInfo> OnOffline { get; } = new SimpleSubPushHandler<RegisterCacheInfo>();
        private readonly WheelTimer<IConnection> wheelTimer = new WheelTimer<IConnection>();

        //private readonly IRateLimit<IPAddress> rateLimit = new TokenBucketRatelimit<IPAddress>();

        public int Count { get => cache.Count; }

        public ClientRegisterCaching(Config config, IUdpServer udpServer, ITcpServer tcpServer)
        {
            this.config = config;

            //rateLimit.Init(5, RateLimitTimeType.Minute);

            tcpServer.OnDisconnect.Sub(Disconnected);
            udpServer.OnDisconnect.Sub(Disconnected);
            tcpServer.OnConnected = AddConnectedTimeout;
            udpServer.OnConnected = AddConnectedTimeout;
        }

        /// <summary>
        /// 超时未注册
        /// </summary>
        /// <param name="connection"></param>
        private void AddConnectedTimeout(IConnection connection)
        {
            wheelTimer.NewTimeout(new WheelTimerTimeoutTask<IConnection>
            {
                Callback = ConnectionTimeoutCallback,
                State = connection
            }, config.RegisterTimeout, false);

            //if (rateLimit.Try(connection.Address.Address, 1) == 0)
            //{
            //    connection.Disponse();
            //}
            //else
            //{
            //    wheelTimer.NewTimeout(new WheelTimerTimeoutTask<IConnection>
            //    {
            //        Callback = ConnectionTimeoutCallback,
            //        State = connection
            //    }, config.RegisterTimeout, false);
            //}
        }
        private void ConnectionTimeoutCallback(WheelTimerTimeout<IConnection> timeout)
        {
            IConnection connection = timeout.Task.State;
            if (connection != null && connection.ConnectId == 0)
            {
                connection.Disponse();
            }
        }

        /// <summary>
        /// 掉线
        /// </summary>
        /// <param name="connection"></param>
        private void Disconnected(IConnection connection)
        {
            if (connection.ConnectId > 0)
            {
                Remove(connection.ConnectId);
            }
        }

        /// <summary>
        /// 连接超时
        /// </summary>
        /// <param name="connection"></param>
        private void AddTimeout(IConnection connection)
        {
            wheelTimer.NewTimeout(new WheelTimerTimeoutTask<IConnection> { Callback = TimeoutCallback, }, 1000, true);
        }
        private void TimeoutCallback(WheelTimerTimeout<IConnection> timeout)
        {
            long time = DateTimeHelper.GetTimeStamp();
            if (timeout.Task.State.IsTimeout(time, config.TimeoutDelay))
            {
                timeout.Cancel();
                if (timeout.Task.State.ConnectId > 0)
                {
                    Remove(timeout.Task.State.ConnectId);
                }
                else
                {
                    timeout.Task.State.Disponse();
                }
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
            cache.AddOrUpdate(model.Id, model, (a, b) => model);
            return model.Id;
        }

        public bool Get(ulong id, out RegisterCacheInfo client)
        {
            return cache.TryGetValue(id, out client);
        }
        public IEnumerable<RegisterCacheInfo> GetBySameGroup(string groupid)
        {
            return cache.Values.Where(c => c.GroupId == groupid);
        }
        public IEnumerable<RegisterCacheInfo> GetBySameGroup(string groupid, string name)
        {
            return cache.Values.Where(c => c.GroupId == groupid && c.Name == name);
        }
        public List<RegisterCacheInfo> GetAll()
        {
            return cache.Values.ToList();
        }
        public RegisterCacheInfo GetByName(string name)
        {
            return cache.Values.FirstOrDefault(c => c.Name == name);
        }

        public void Remove(ulong id)
        {
            if (cache.TryRemove(id, out RegisterCacheInfo client))
            {
                if (client != null)
                {
                    client.UdpConnection?.Disponse();
                    client.TcpConnection?.Disponse();
                    OnChanged.Push(client);
                    OnOffline.Push(client);
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
