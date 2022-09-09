using client.messengers.clients;
using common.libs;
using common.server;
using common.server.model;
using common.server.servers.rudp;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace client.realize.messengers.clients
{
    public class ClientInfoCaching : IClientInfoCaching
    {
        private readonly ConcurrentDictionary<ulong, ClientInfo> clients = new ConcurrentDictionary<ulong, ClientInfo>();
        private readonly ConcurrentDictionary<ulong, int> tunnelPorts = new ConcurrentDictionary<ulong, int>();
        private readonly ConcurrentDictionary<ulong, UdpServer> udpservers = new ConcurrentDictionary<ulong, UdpServer>();

        public SimpleSubPushHandler<ClientInfo> OnOffline { get; } = new SimpleSubPushHandler<ClientInfo>();
        public SimpleSubPushHandler<ClientInfo> OnOnline { get; } = new SimpleSubPushHandler<ClientInfo>();

        public bool Add(ClientInfo client)
        {
            return clients.TryAdd(client.Id, client);
        }

        public bool Get(ulong id, out ClientInfo client)
        {
            return clients.TryGetValue(id, out client);
        }

        public ClientInfo GetByName(string name)
        {
            return clients.Values.FirstOrDefault(c => c.Name == name);
        }

        public IEnumerable<ClientInfo> All()
        {
            return clients.Values;
        }

        public IEnumerable<ulong> AllIds()
        {
            return clients.Keys;
        }

        public void Connecting(ulong id, bool val, ServerType serverType)
        {
            if (clients.TryGetValue(id, out ClientInfo client))
            {
                client.Connecting(val, serverType);
            }
        }

        public void Offline(ulong id)
        {
            if (clients.TryGetValue(id, out ClientInfo client))
            {
                client.OfflineUdp();
                client.OfflineTcp();
                OnOffline.Push(client);
            }
        }
        public void Offline(ulong id, ServerType serverType)
        {
            if (clients.TryGetValue(id, out ClientInfo client))
            {
                client.Offline(serverType);
                OnOffline.Push(client);
            }
        }

        public void Remove(ulong id)
        {
            clients.TryRemove(id, out _);
        }

        public void Online(ulong id, IConnection connection, ClientConnectTypes connectType)
        {
            if (clients.TryGetValue(id, out ClientInfo client))
            {
                connection.ConnectId = id;
                client.Online(connection, connectType);
                OnOnline.Push(client);
            }
        }

        public void AddTunnelPort(ulong tunnelName, int port)
        {
            tunnelPorts.TryAdd(tunnelName, port);
        }
        public bool GetTunnelPort(ulong tunnelName,out int port)
        {
            return tunnelPorts.TryGetValue(tunnelName,out port);
        }
        public void RemoveTunnelPort(ulong tunnelName)
        {
            tunnelPorts.TryRemove(tunnelName, out _);
        }

        public void AddUdpserver(ulong tunnelName, UdpServer server)
        {
            udpservers.TryAdd(tunnelName, server);
        }
        public bool GetUdpserver(ulong tunnelName, out UdpServer server)
        {
            return udpservers.TryGetValue(tunnelName, out server);
        }
        public void RemoveUdpserver(ulong tunnelName)
        {
            udpservers.TryRemove(tunnelName, out _);
        }

        public void Clear()
        {
            var _clients = clients.Values;
            foreach (var item in _clients)
            {
                item.OfflineUdp();
                item.OfflineTcp();
                OnOffline.Push(item);
                clients.TryRemove(item.Id, out _);
            }
            tunnelPorts.Clear();
        }
    }
}
