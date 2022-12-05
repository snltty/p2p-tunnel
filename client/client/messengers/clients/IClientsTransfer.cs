using common.server;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace client.messengers.clients
{
    /// <summary>
    /// 客户单操作
    /// </summary>
    public interface IClientsTransfer
    {
        /// <summary>
        /// 连接
        /// </summary>
        /// <param name="id"></param>
        public void ConnectClient(ulong id);
        /// <summary>
        /// 连接
        /// </summary>
        /// <param name="info"></param>
        public void ConnectClient(ClientInfo info);
        /// <summary>
        /// 连我
        /// </summary>
        /// <param name="info"></param>
        public void ConnectReverse(ClientInfo info);
        /// <summary>
        /// 连我
        /// </summary>
        /// <param name="id"></param>
        public void ConnectReverse(ulong id);
        /// <summary>
        /// 重启
        /// </summary>
        /// <param name="id"></param>
        public void Reset(ulong id);
        /// <summary>
        /// 停止打洞
        /// </summary>
        /// <param name="id"></param>
        public void ConnectStop(ulong id);
        /// <summary>
        /// ping
        /// </summary>
        /// <returns></returns>
        public Task Ping();

        /// <summary>
        /// 各个客户端连接信息
        /// </summary>
        /// <returns></returns>
        public Task<ConcurrentDictionary<ulong, ulong[]>> Connects();
        /// <summary>
        /// 所有可中继线路的延迟
        /// </summary>
        /// <param name="paths"></param>
        /// <returns></returns>
        public Task<int[]> Delay(ulong[][] paths);
        /// <summary>
        /// 中继
        /// </summary>
        /// <param name="sourceConnection">通过谁中继</param>
        /// <param name="relayids">中继线路</param>
        /// <param name="notify">是否通知对方</param>
        /// <returns></returns>
        public Task Relay(IConnection sourceConnection, ulong[] relayids, bool notify = false);
    }
}
