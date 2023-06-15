using client.service.ui.api.clientServer;
using common.forward;
using common.libs.extends;
using common.proxy;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace client.service.forward
{
    /// <summary>
    /// tcp转发
    /// </summary>
    public sealed class ForwardClientService : IClientService
    {
        private readonly ForwardTransfer forwardTransfer;
        private readonly IProxyServer proxyServer;
        private readonly common.forward.Config config;
        public ForwardClientService(ForwardTransfer forwardTransfer, IProxyServer proxyServer, common.forward.Config config)
        {
            this.forwardTransfer = forwardTransfer;
            this.proxyServer = proxyServer;
            this.config = config;
        }
        /// <summary>
        /// 添加监听
        /// </summary>
        /// <param name="arg"></param>
        public bool AddListen(ClientServiceParamsInfo arg)
        {
            P2PListenAddParams fmodel = arg.Content.DeJson<P2PListenAddParams>();

            string errmsg = forwardTransfer.AddP2PListen(fmodel);
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
            forwardTransfer.RemoveP2PListen(uint.Parse(arg.Content));
            return true;
        }
        /// <summary>
        /// 添加转发
        /// </summary>
        /// <param name="arg"></param>
        public void AddForward(ClientServiceParamsInfo arg)
        {
            P2PForwardAddParams fmodel = arg.Content.DeJson<P2PForwardAddParams>();
            string errmsg = forwardTransfer.AddP2PForward(fmodel);
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
            forwardTransfer.RemoveP2PForward(fmodel);
        }

        /// <summary>
        /// 监听列表
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public IEnumerable<P2PListenInfo> List(ClientServiceParamsInfo arg)
        {
            return forwardTransfer.p2pListens;
        }

        /// <summary>
        /// 获取监听
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public P2PListenInfo Get(ClientServiceParamsInfo arg)
        {
            return forwardTransfer.GetP2PByID(uint.Parse(arg.Content));
        }
        /// <summary>
        /// 开启监听
        /// </summary>
        /// <param name="arg"></param>
        public bool Start(ClientServiceParamsInfo arg)
        {
            string errmsg = forwardTransfer.StartP2P(uint.Parse(arg.Content));
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
            string errmsg = forwardTransfer.StopP2P(uint.Parse(arg.Content));
            if (string.IsNullOrWhiteSpace(errmsg) == false)
            {
                arg.SetCode(ClientServiceResponseCodes.Error, errmsg);
            }
            return true;
        }

        public async Task<EnumProxyCommandStatusMsg> Test(ClientServiceParamsInfo arg)
        {
            TestTargetInfo fmodel = arg.Content.DeJson<TestTargetInfo>();
            return await forwardTransfer.Test(fmodel.Host, fmodel.Port);
        }
    }

    public sealed class TestTargetInfo
    {
        public string Host { get; set; }
        public int Port { get; set; }
    }
}
