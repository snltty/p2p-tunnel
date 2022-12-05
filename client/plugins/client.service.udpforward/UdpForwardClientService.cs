using client.service.ui.api.clientServer;
using common.libs.extends;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace client.service.udpforward
{
    /// <summary>
    /// udp转发相关
    /// </summary>
    public sealed class UdpForwardClientService : IClientService
    {
        private readonly UdpForwardTransfer udpForwardTransfer;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="udpForwardTransfer"></param>
        public UdpForwardClientService(UdpForwardTransfer udpForwardTransfer)
        {
            this.udpForwardTransfer = udpForwardTransfer;
        }

        /// <summary>
        /// 添加监听
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        [ClientService(typeof(P2PListenInfo))]
        public bool AddListen(ClientServiceParamsInfo arg)
        {
            P2PListenInfo fmodel = arg.Content.DeJson<P2PListenInfo>();

            string errmsg = udpForwardTransfer.AddP2PListen(fmodel);
            if (string.IsNullOrWhiteSpace(errmsg) == false)
            {
                arg.SetCode(ClientServiceResponseCodes.Error, errmsg);
            }
            return true;
        }

        /// <summary>
        /// 删除监听
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        [ClientService(typeof(ushort))]
        public bool RemoveListen(ClientServiceParamsInfo arg)
        {
            udpForwardTransfer.RemoveP2PListen(ushort.Parse(arg.Content));
            return true;
        }

        /// <summary>
        /// 监听列表
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        [ClientService(null)]
        public IEnumerable<P2PListenInfo> List(ClientServiceParamsInfo arg)
        {
            return udpForwardTransfer.p2PConfigInfo.Tunnels;
        }

        /// <summary>
        /// 获取单个监听
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        [ClientService(typeof(ushort))]
        public P2PListenInfo Get(ClientServiceParamsInfo arg)
        {
            return udpForwardTransfer.GetP2PByPort(ushort.Parse(arg.Content));
        }

        /// <summary>
        /// 开启监听
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        [ClientService(typeof(ushort))]
        public bool Start(ClientServiceParamsInfo arg)
        {
            string errmsg = udpForwardTransfer.StartP2P(ushort.Parse(arg.Content));
            if (string.IsNullOrWhiteSpace(errmsg) == false)
            {
                arg.SetCode(ClientServiceResponseCodes.Error, errmsg);
            }
            return true;
        }

        /// <summary>
        /// 停止监听
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        [ClientService(typeof(ushort))]
        public bool Stop(ClientServiceParamsInfo arg)
        {
            udpForwardTransfer.StopP2PListen(ushort.Parse(arg.Content));

            return true;
        }

        /// <summary>
        /// 服务端转发列表
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        [ClientService(null)]
        public List<ServerForwardItemInfo> ServerForwards(ClientServiceParamsInfo arg)
        {
            return udpForwardTransfer.serverConfigInfo.Tunnels;
        }

        /// <summary>
        /// 服务端可用端口
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        [ClientService(null)]
        public async Task<ushort[]> ServerPorts(ClientServiceParamsInfo arg)
        {
            return await udpForwardTransfer.GetServerPorts();
        }

        /// <summary>
        /// 服务端添加转发
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        [ClientService(typeof(ServerForwardItemInfo))]
        public async Task<bool> AddServerForward(ClientServiceParamsInfo arg)
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
        public async Task<bool> StartServerForward(ClientServiceParamsInfo arg)
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
        public async Task<bool> StopServerForward(ClientServiceParamsInfo arg)
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
        public async Task<bool> RemoveServerForward(ClientServiceParamsInfo arg)
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
