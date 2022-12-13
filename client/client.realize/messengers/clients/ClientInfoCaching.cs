using client.messengers.clients;
using common.libs;
using common.libs.extends;
using common.server;
using common.server.servers.rudp;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace client.realize.messengers.clients
{
    /// <summary>
    /// 客户单缓存
    /// </summary>
    public sealed class ClientInfoCaching : IClientInfoCaching
    {
        private readonly ConcurrentDictionary<ulong, ClientInfo> clients = new ConcurrentDictionary<ulong, ClientInfo>();
        private readonly ConcurrentDictionary<string, ClientInfo> clientsByName = new ConcurrentDictionary<string, ClientInfo>();
        private readonly ConcurrentDictionary<ulong, int> tunnelPorts = new ConcurrentDictionary<ulong, int>();
        private readonly ConcurrentDictionary<ulong, UdpServer> udpservers = new ConcurrentDictionary<ulong, UdpServer>();

        /// <summary>
        /// 下线
        /// </summary>
        public SimpleSubPushHandler<ClientInfo> OnOffline { get; } = new SimpleSubPushHandler<ClientInfo>();
        /// <summary>
        /// 下线后
        /// </summary>
        public SimpleSubPushHandler<ClientInfo> OnOfflineAfter { get; } = new SimpleSubPushHandler<ClientInfo>();
        /// <summary>
        /// 上线
        /// </summary>
        public SimpleSubPushHandler<ClientInfo> OnOnline { get; } = new SimpleSubPushHandler<ClientInfo>();
        /// <summary>
        /// 添加
        /// </summary>
        public SimpleSubPushHandler<ClientInfo> OnAdd { get; } = new SimpleSubPushHandler<ClientInfo>();
        /// <summary>
        /// 删除
        /// </summary>
        public SimpleSubPushHandler<ClientInfo> OnRemove { get; } = new SimpleSubPushHandler<ClientInfo>();

        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public bool Add(ClientInfo client)
        {
            bool result = clients.TryAdd(client.Id, client);
            if (result)
            {
                clientsByName.TryAdd(client.Name, client);
            }

            OnAdd.Push(client);
            return result;
        }

        /// <summary>
        /// 获取
        /// </summary>
        /// <param name="id"></param>
        /// <param name="client"></param>
        /// <returns></returns>
        public bool Get(ulong id, out ClientInfo client)
        {
            return clients.TryGetValue(id, out client);
        }
        /// <summary>
        /// 根据名字获取
        /// </summary>
        /// <param name="name"></param>
        /// <param name="client"></param>
        /// <returns></returns>
        public bool GetByName(string name, out ClientInfo client)
        {
            return clientsByName.TryGetValue(name, out client);
        }

        /// <summary>
        /// 所有客户端
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ClientInfo> All()
        {
            return clients.Values;
        }
        /// <summary>
        /// 所有客户端id
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ulong> AllIds()
        {
            return clients.Keys;
        }

        /// <summary>
        /// 设置状态
        /// </summary>
        /// <param name="id"></param>
        /// <param name="val"></param>
        public void SetConnecting(ulong id, bool val)
        {
            if (clients.TryGetValue(id, out ClientInfo client))
            {
                client.SetConnecting(val);
            }
        }

        /// <summary>
        /// 下线
        /// </summary>
        /// <param name="id"></param>
        /// <param name="offlineType"></param>
        public void Offline(ulong id, ClientOfflineTypes offlineType = ClientOfflineTypes.Manual)
        {
            if (clients.TryGetValue(id, out ClientInfo client))
            {
                if (client.ConnectType != ClientConnectTypes.Unknow)
                {
                    OnOffline.Push(client);
                    client.Offline(offlineType);
                    OnOfflineAfter.Push(client);
                }
            }
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="id"></param>
        public void Remove(ulong id)
        {
            if (clients.TryRemove(id, out ClientInfo client))
            {
                client.Offline();
                clientsByName.TryRemove(client.Name, out _);
                udpservers.TryRemove(id, out _);
                tunnelPorts.TryRemove(id, out _);
                OnRemove.Push(client);
            }
        }

        /// <summary>
        /// 上线
        /// </summary>
        /// <param name="id"></param>
        /// <param name="connection"></param>
        /// <param name="connectType"></param>
        /// <param name="onlineType"></param>
        public void Online(ulong id, IConnection connection, ClientConnectTypes connectType, ClientOnlineTypes onlineType, ulong tunnelName)
        {
            if (clients.TryGetValue(id, out ClientInfo client))
            {
                connection.ConnectId = id;
                client.Online(connection, connectType, onlineType, tunnelName);
                //RemoveTunnelPort(tunnelName);
                //RemoveUdpserver(tunnelName);
                OnOnline.Push(client);
            }
        }

        /// <summary>
        /// 新端口
        /// </summary>
        /// <param name="tunnelName"></param>
        /// <param name="port"></param>
        public void AddTunnelPort(ulong tunnelName, int port)
        {
            tunnelPorts.TryAdd(tunnelName, port);
        }
        /// <summary>
        /// 新端口
        /// </summary>
        /// <param name="tunnelName"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public bool GetTunnelPort(ulong tunnelName, out int port)
        {
            return tunnelPorts.TryGetValue(tunnelName, out port);
        }
        /// <summary>
        /// 删除新端口
        /// </summary>
        /// <param name="tunnelName"></param>
        public void RemoveTunnelPort(ulong tunnelName)
        {
            tunnelPorts.TryRemove(tunnelName, out _);
        }

        /// <summary>
        /// 新通道
        /// </summary>
        /// <param name="tunnelName"></param>
        /// <param name="server"></param>
        public void AddUdpserver(ulong tunnelName, UdpServer server)
        {
            udpservers.TryAdd(tunnelName, server);
        }
        /// <summary>
        /// 新通道
        /// </summary>
        /// <param name="tunnelName"></param>
        /// <param name="server"></param>
        /// <returns></returns>
        public bool GetUdpserver(ulong tunnelName, out UdpServer server)
        {
            return udpservers.TryGetValue(tunnelName, out server);
        }
        /// <summary>
        /// 删除新通道
        /// </summary>
        /// <param name="tunnelName"></param>
        /// <param name="clear"></param>
        public void RemoveUdpserver(ulong tunnelName, bool clear = false)
        {
            if (udpservers.TryRemove(tunnelName, out UdpServer server) && server != null && clear)
            {
                server.Disponse();
            }
        }

        /// <summary>
        /// 清除所有
        /// </summary>
        public void Clear()
        {
            var _clients = clients.Values;
            foreach (var item in _clients)
            {
                OnOffline.Push(item);
                item.Offline(ClientOfflineTypes.Manual);
                OnOfflineAfter.Push(item);
                clients.TryRemove(item.Id, out _);
                OnRemove.Push(item);
            }
            udpservers.Clear();
            tunnelPorts.Clear();
            clientsByName.Clear();
        }
    }
}
