using client.messengers.clients;
using common.libs;
using common.libs.extends;
using common.server;
using common.server.servers.rudp;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace client.realize.messengers.clients
{
    public class ClientInfoCaching : IClientInfoCaching
    {
        private readonly ConcurrentDictionary<ulong, ClientInfo> clients = new ConcurrentDictionary<ulong, ClientInfo>();
        private readonly ConcurrentDictionary<string, ClientInfo> clientsByName = new ConcurrentDictionary<string, ClientInfo>();
        private readonly ConcurrentDictionary<ulong, int> tunnelPorts = new ConcurrentDictionary<ulong, int>();
        private readonly ConcurrentDictionary<ulong, UdpServer> udpservers = new ConcurrentDictionary<ulong, UdpServer>();

        public SimpleSubPushHandler<ClientInfo> OnOffline { get; } = new SimpleSubPushHandler<ClientInfo>();
        public SimpleSubPushHandler<ClientInfo> OnOnline { get; } = new SimpleSubPushHandler<ClientInfo>();
        public SimpleSubPushHandler<ClientInfo> OnAdd { get; } = new SimpleSubPushHandler<ClientInfo>();
        public SimpleSubPushHandler<ClientInfo> OnRemove { get; } = new SimpleSubPushHandler<ClientInfo>();

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

        public bool Get(ulong id, out ClientInfo client)
        {
            return clients.TryGetValue(id, out client);
        }

        public bool GetByName(string name, out ClientInfo client)
        {
            return clientsByName.TryGetValue(name, out client);
        }

        public IEnumerable<ClientInfo> All()
        {
            return clients.Values;
        }

        public IEnumerable<ulong> AllIds()
        {
            return clients.Keys;
        }

        public void SetConnecting(ulong id, bool val)
        {
            if (clients.TryGetValue(id, out ClientInfo client))
            {
                client.SetConnecting(val);
            }
        }

        public void Offline(ulong id, ClientOfflineTypes offlineType = ClientOfflineTypes.Manual)
        {
            if (clients.TryGetValue(id, out ClientInfo client))
            {
                if (client.ConnectType != ClientConnectTypes.Unknow)
                {
                    client.Offline(offlineType);
                    OnOffline.Push(client);
                }
            }
            foreach (ClientInfo _client in clients.Values.Where(c => c.Connected && c.ConnectType == ClientConnectTypes.RelayNode && c.Connection.RelayId.Contains(id)))
            {
                if (client.ConnectType != ClientConnectTypes.Unknow)
                {
                    _client.Offline(offlineType);
                    OnOffline.Push(_client);
                }
            }
        }

        public void Remove(ulong id)
        {
            if (clients.TryRemove(id, out ClientInfo client))
            {
                clientsByName.TryRemove(client.Name, out _);
                udpservers.TryRemove(id, out _);
                tunnelPorts.TryRemove(id, out _);
                OnRemove.Push(client);
            }
        }

        public void Online(ulong id, IConnection connection, ClientConnectTypes connectType, ClientOnlineTypes onlineType)
        {
            if (clients.TryGetValue(id, out ClientInfo client))
            {
                connection.ConnectId = id;
                client.Online(connection, connectType, onlineType);
                OnOnline.Push(client);
            }
        }

        public void AddTunnelPort(ulong tunnelName, int port)
        {
            tunnelPorts.TryAdd(tunnelName, port);
        }
        public bool GetTunnelPort(ulong tunnelName, out int port)
        {
            return tunnelPorts.TryGetValue(tunnelName, out port);
        }
        public void RemoveTunnelPort(ulong tunnelName)
        {
            tunnelPorts.TryRemove(tunnelName, out _);
        }
        public string TunnelPorts()
        {
            return tunnelPorts.ToJson();
        }


        public void AddUdpserver(ulong tunnelName, UdpServer server)
        {
            udpservers.TryAdd(tunnelName, server);
        }
        public bool GetUdpserver(ulong tunnelName, out UdpServer server)
        {
            return udpservers.TryGetValue(tunnelName, out server);
        }
        public void RemoveUdpserver(ulong tunnelName, bool clear = false)
        {
            if (udpservers.TryRemove(tunnelName, out UdpServer server) && server != null && clear)
            {
                server.Disponse();
            }
        }

        public void Clear()
        {
            var _clients = clients.Values;
            foreach (var item in _clients)
            {
                item.Offline(ClientOfflineTypes.Manual);
                OnOffline.Push(item);
                clients.TryRemove(item.Id, out _);
                OnRemove.Push(item);
            }
            udpservers.Clear();
            tunnelPorts.Clear();
            clientsByName.Clear();
        }
    }
}
