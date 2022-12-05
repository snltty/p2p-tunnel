using client.service.ui.api.clientServer;
using common.libs.extends;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace client.service.tcpforward
{
    /// <summary>
    /// tcp转发
    /// </summary>
    public sealed class TcpForwardClientService : IClientService
    {
        private readonly TcpForwardTransfer tcpForwardTransfer;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tcpForwardTransfer"></param>
        public TcpForwardClientService(TcpForwardTransfer tcpForwardTransfer)
        {
            this.tcpForwardTransfer = tcpForwardTransfer;
        }
        /// <summary>
        /// 添加监听
        /// </summary>
        /// <param name="arg"></param>
        public bool AddListen(ClientServiceParamsInfo arg)
        {
            P2PListenAddParams fmodel = arg.Content.DeJson<P2PListenAddParams>();

            string errmsg = tcpForwardTransfer.AddP2PListen(fmodel);
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
        public bool RemoveListen(ClientServiceParamsInfo arg)
        {
            tcpForwardTransfer.RemoveP2PListen(uint.Parse(arg.Content));
            return true;
        }
        /// <summary>
        /// 添加转发
        /// </summary>
        /// <param name="arg"></param>
        public void AddForward(ClientServiceParamsInfo arg)
        {
            P2PForwardAddParams fmodel = arg.Content.DeJson<P2PForwardAddParams>();
            string errmsg = tcpForwardTransfer.AddP2PForward(fmodel);
            if (string.IsNullOrWhiteSpace(errmsg) == false)
            {
                arg.SetCode(ClientServiceResponseCodes.Error, errmsg);
            }
        }
        /// <summary>
        /// 删除转发
        /// </summary>
        /// <param name="arg"></param>
        public void RemoveForward(ClientServiceParamsInfo arg)
        {
            P2PForwardRemoveParams fmodel = arg.Content.DeJson<P2PForwardRemoveParams>();
            tcpForwardTransfer.RemoveP2PForward(fmodel);
        }
        /// <summary>
        /// 监听列表
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public IEnumerable<P2PListenInfo> List(ClientServiceParamsInfo arg)
        {
            return tcpForwardTransfer.p2pListens.Where(c => c.ForwardType == common.tcpforward.TcpForwardTypes.Forward);
        }
        /// <summary>
        /// 代理
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public P2PListenInfo ListProxy(ClientServiceParamsInfo arg)
        {
            return tcpForwardTransfer.p2pListens.FirstOrDefault(c => c.ForwardType == common.tcpforward.TcpForwardTypes.Proxy);
        }
        /// <summary>
        /// 获取pac
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public string GetPac(ClientServiceParamsInfo arg)
        {
            return tcpForwardTransfer.GetPac();
        }
        /// <summary>
        /// 获取监听
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public P2PListenInfo Get(ClientServiceParamsInfo arg)
        {
            return tcpForwardTransfer.GetP2PByID(uint.Parse(arg.Content));
        }
        /// <summary>
        /// 开启监听
        /// </summary>
        /// <param name="arg"></param>
        public bool Start(ClientServiceParamsInfo arg)
        {
            string errmsg = tcpForwardTransfer.StartP2P(uint.Parse(arg.Content));
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
        public bool Stop(ClientServiceParamsInfo arg)
        {
            string errmsg = tcpForwardTransfer.StopP2P(uint.Parse(arg.Content));
            if (string.IsNullOrWhiteSpace(errmsg) == false)
            {
                arg.SetCode(ClientServiceResponseCodes.Error, errmsg);
            }
            return true;
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
