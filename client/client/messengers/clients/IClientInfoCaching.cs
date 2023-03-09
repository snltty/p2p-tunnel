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
        /// <summary>
        /// 掉线
        /// </summary>
        public SimpleSubPushHandler<ClientInfo> OnOffline { get; }
        /// <summary>
        /// 掉线后
        /// </summary>
        public SimpleSubPushHandler<ClientInfo> OnOfflineAfter { get; }
        /// <summary>
        /// 上线
        /// </summary>
        public SimpleSubPushHandler<ClientInfo> OnOnline { get; }
        /// <summary>
        /// 添加
        /// </summary>
        public SimpleSubPushHandler<ClientInfo> OnAdd { get; }
        /// <summary>
        /// 删除
        /// </summary>
        public SimpleSubPushHandler<ClientInfo> OnRemove { get; }

        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public bool Add(ClientInfo client);
        /// <summary>
        /// 获取
        /// </summary>
        /// <param name="id"></param>
        /// <param name="client"></param>
        /// <returns></returns>
        public bool Get(ulong id, out ClientInfo client);
        /// <summary>
        /// 按名获取
        /// </summary>
        /// <param name="name"></param>
        /// <param name="client"></param>
        /// <returns></returns>
        public bool GetByName(string name, out ClientInfo client);
        /// <summary>
        /// 所有
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ClientInfo> All();
        /// <summary>
        /// 所有id
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ulong> AllIds();
        /// <summary>
        /// 设置连接状态
        /// </summary>
        /// <param name="id"></param>
        /// <param name="val"></param>
        public void SetConnecting(ulong id,bool val);
        /// <summary>
        /// 掉线
        /// </summary>
        /// <param name="id"></param>
        /// <param name="offlineType"></param>
        public void Offline(ulong id,ClientOfflineTypes offlineType = ClientOfflineTypes.Manual);
        /// <summary>
        /// 上线
        /// </summary>
        /// <param name="id"></param>
        /// <param name="connection"></param>
        /// <param name="connectType"></param>
        /// <param name="onlineType"></param>
        public void Online(ulong id, IConnection connection, ClientConnectTypes connectType, ClientOnlineTypes onlineType);
        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="id"></param>
        public void Remove(ulong id);

        /// <summary>
        /// 新通道端口
        /// </summary>
        /// <param name="id"></param>
        /// <param name="port"></param>
        public void AddTunnelPort(ulong id, int port);
        /// <summary>
        /// 获取通道端口
        /// </summary>
        /// <param name="id"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public bool GetTunnelPort(ulong id, out int port);

        /// <summary>
        /// 通道服务端
        /// </summary>
        /// <param name="id"></param>
        /// <param name="server"></param>
        public void AddUdpserver(ulong id, UdpServer server);
        /// <summary>
        /// 通道服务端
        /// </summary>
        /// <param name="id"></param>
        /// <param name="server"></param>
        /// <returns></returns>
        public bool GetUdpserver(ulong id, out UdpServer server);
        /// <summary>
        /// 删除通道服务端
        /// </summary>
        /// <param name="id"></param>
        /// <param name="clear"></param>
        public void RemoveUdpserver(ulong id, bool clear = false);

        /// <summary>
        /// 清除所有
        /// </summary>
        public void Clear();
    }
}
