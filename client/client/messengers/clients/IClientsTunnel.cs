using common.server;
using common.server.model;
using System;
using System.Threading.Tasks;

namespace client.messengers.clients
{
    /// <summary>
    /// 客户端打洞新通道
    /// </summary>
    public interface IClientsTunnel
    {
        /// <summary>
        /// 通道关闭
        /// </summary>
        public Action<IConnection, IConnection> OnDisConnect { get; set; }
        /// <summary>
        /// 新通道
        /// </summary>
        /// <param name="serverType"></param>
        /// <param name="tunnelName"></param>
        /// <returns>通道名，端口</returns>
        public Task<(ulong, ushort)> NewBind(ServerType serverType, ulong tunnelName);
    }
}
