using common.server.model;
using System.Collections.Concurrent;

namespace client.messengers.relay
{
    /// <summary>
    /// 中继的各个客户端连接信息缓存
    /// </summary>
    public interface IClientConnectsCaching
    {
        /// <summary>
        /// 连接信息
        /// </summary>
        public ConcurrentDictionary<ulong, ulong[]> Connects { get; }

        /// <summary>
        /// 添加连接信息
        /// </summary>
        /// <param name="connects"></param>
        public void AddConnects(ConnectsInfo connects);

        /// <summary>
        /// 移除某个客户端的
        /// </summary>
        /// <param name="id"></param>
        public void Remove(ulong id);
        /// <summary>
        /// 清除全部
        /// </summary>
        public void Clear();
    }
}
