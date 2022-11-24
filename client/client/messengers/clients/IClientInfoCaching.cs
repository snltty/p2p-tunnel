using common.libs;
using common.server;
using common.server.servers.rudp;
using System.Collections.Generic;

namespace client.messengers.clients
{
    /// <summary>
    /// 客户端缓存
    /// </summary>
    public interface IClientInfoCaching
    {
        public SimpleSubPushHandler<ClientInfo> OnOffline { get; }
        public SimpleSubPushHandler<ClientInfo> OnOnline { get; }
        public SimpleSubPushHandler<ClientInfo> OnAdd { get; }
        public SimpleSubPushHandler<ClientInfo> OnRemove { get; }
        public bool Add(ClientInfo client);
        public bool Get(ulong id, out ClientInfo client);
        public bool GetByName(string name, out ClientInfo client);
        public IEnumerable<ClientInfo> All();
        public IEnumerable<ulong> AllIds();
        public void SetConnecting(ulong id,bool val);
        public void Offline(ulong id, ClientOfflineTypes offlineType = ClientOfflineTypes.Manual);
        public void Online(ulong id, IConnection connection, ClientConnectTypes connectType, ClientOnlineTypes onlineType);
        public void Remove(ulong id);

        public void AddTunnelPort(ulong tunnelName, int port);
        public bool GetTunnelPort(ulong tunnelName, out int port);
        public void RemoveTunnelPort(ulong tunnelName);
        public string TunnelPorts();

        public void AddUdpserver(ulong tunnelName, UdpServer server);
        public bool GetUdpserver(ulong tunnelName, out UdpServer server);
        public void RemoveUdpserver(ulong tunnelName,bool clear = false);

        public void Clear();
    }
}
