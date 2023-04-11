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
       
    }
}
