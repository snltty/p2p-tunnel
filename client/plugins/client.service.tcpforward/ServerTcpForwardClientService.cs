using client.service.ui.api.clientServer;
using common.libs.extends;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace client.service.tcpforward
{
    /// <summary>
    /// 服务器tcp转发
    /// </summary>
    public sealed class ServerTcpForwardClientService : IClientService
    {
        private readonly TcpForwardTransfer tcpForwardTransfer;
        public ServerTcpForwardClientService(TcpForwardTransfer tcpForwardTransfer)
        {
            this.tcpForwardTransfer = tcpForwardTransfer;
        }
       
        /// <summary>
        /// 服务器转发
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public List<ServerForwardItemInfo> ServerForwards(ClientServiceParamsInfo arg)
        {
            return tcpForwardTransfer.serverForwards;
        }
        /// <summary>
        /// 服务器端口
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public async Task<ushort[]> ServerPorts(ClientServiceParamsInfo arg)
        {
            return await tcpForwardTransfer.GetServerPorts();
        }
        /// <summary>
        /// 服务器转发添加
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public async Task<bool> AddServerForward(ClientServiceParamsInfo arg)
        {
            ServerForwardItemInfo forward = arg.Content.DeJson<ServerForwardItemInfo>();
            string res = await tcpForwardTransfer.AddServerForward(forward);
            if (string.IsNullOrWhiteSpace(res) == false)
            {
                arg.SetErrorMessage(res);
            }
            return true;
        }
        /// <summary>
        /// 服务器转发开启
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public async Task<bool> StartServerForward(ClientServiceParamsInfo arg)
        {
            ServerForwardItemInfo forward = arg.Content.DeJson<ServerForwardItemInfo>();
            string res = await tcpForwardTransfer.StartServerForward(forward);
            if (string.IsNullOrWhiteSpace(res) == false)
            {
                arg.SetErrorMessage(res);
            }
            return true;
        }
        /// <summary>
        /// 服务器转发停止
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public async Task<bool> StopServerForward(ClientServiceParamsInfo arg)
        {
            ServerForwardItemInfo forward = arg.Content.DeJson<ServerForwardItemInfo>();
            string res = await tcpForwardTransfer.StopServerForward(forward);
            if (string.IsNullOrWhiteSpace(res) == false)
            {
                arg.SetErrorMessage(res);
            }
            return true;
        }
        /// <summary>
        /// 服务器转发删除
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public async Task<bool> RemoveServerForward(ClientServiceParamsInfo arg)
        {
            ServerForwardItemInfo forward = arg.Content.DeJson<ServerForwardItemInfo>();
            string res = await tcpForwardTransfer.RemoveServerForward(forward);
            if (string.IsNullOrWhiteSpace(res) == false)
            {
                arg.SetErrorMessage(res);
            }
            return true;
        }
    }
}
