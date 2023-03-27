using client.service.ui.api.clientServer;
using common.libs.extends;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace client.service.udpforward.server
{
    /// <summary>
    /// 服务器udp转发相关
    /// </summary>
    public sealed class ServerUdpForwardClientService : IClientService
    {
        private readonly ServerUdpForwardTransfer udpForwardTransfer;
        public ServerUdpForwardClientService(ServerUdpForwardTransfer udpForwardTransfer)
        {
            this.udpForwardTransfer = udpForwardTransfer;
        }

        /// <summary>
        /// 服务端转发列表
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        [ClientService(null)]
        public List<ServerForwardItemInfo> List(ClientServiceParamsInfo arg)
        {
            return udpForwardTransfer.serverConfigInfo.Tunnels;
        }

        /// <summary>
        /// 服务端可用端口
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        [ClientService(null)]
        public async Task<ushort[]> Ports(ClientServiceParamsInfo arg)
        {
            return await udpForwardTransfer.GetServerPorts();
        }

        /// <summary>
        /// 服务端添加转发
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        [ClientService(typeof(ServerForwardItemInfo))]
        public async Task<bool> Add(ClientServiceParamsInfo arg)
        {
            ServerForwardItemInfo forward = arg.Content.DeJson<ServerForwardItemInfo>();
            string res = await udpForwardTransfer.AddServerForward(forward);
            if (string.IsNullOrWhiteSpace(res) == false)
            {
                arg.SetErrorMessage(res);
            }
            return true;
        }

        /// <summary>
        /// 服务端转发开启
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        [ClientService(typeof(ushort))]
        public async Task<bool> Start(ClientServiceParamsInfo arg)
        {
            string res = await udpForwardTransfer.StartServerForward(ushort.Parse(arg.Content));
            if (string.IsNullOrWhiteSpace(res) == false)
            {
                arg.SetErrorMessage(res);
            }
            return true;
        }

        /// <summary>
        /// 服务端转发停止
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        [ClientService(typeof(ushort))]
        public async Task<bool> Stop(ClientServiceParamsInfo arg)
        {
            string res = await udpForwardTransfer.StopServerForward(ushort.Parse(arg.Content));
            if (string.IsNullOrWhiteSpace(res) == false)
            {
                arg.SetErrorMessage(res);
            }
            return true;
        }

        /// <summary>
        /// 服务端转发删除
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        [ClientService(typeof(ushort))]
        public async Task<bool> Remove(ClientServiceParamsInfo arg)
        {
            string res = await udpForwardTransfer.RemoveServerForward(ushort.Parse(arg.Content));
            if (string.IsNullOrWhiteSpace(res) == false)
            {
                arg.SetErrorMessage(res);
            }
            return true;
        }
    }
}
