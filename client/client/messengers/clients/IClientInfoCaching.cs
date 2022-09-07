using common.libs;
using common.server;
using common.server.model;
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
        public bool Add(ClientInfo client);
        public bool Get(ulong id, out ClientInfo client);
        public ClientInfo GetByName(string name);
        public IEnumerable<ClientInfo> All();
        public IEnumerable<ulong> AllIds();
        public void Connecting(ulong id,bool val, ServerType serverType);
        public void Offline(ulong id);
        public void Offline(ulong id,ServerType serverType);
        public void Online(ulong id, IConnection connection, ClientConnectTypes connectType);
        public void Remove(ulong id);
        public void Clear();
    }
}
