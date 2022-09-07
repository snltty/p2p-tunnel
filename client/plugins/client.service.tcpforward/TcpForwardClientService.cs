using client.service.ui.api.clientServer;
using common.libs.extends;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace client.service.tcpforward
{
    public class TcpForwardClientService : IClientService
    {
        private readonly TcpForwardTransfer tcpForwardTransfer;
        public TcpForwardClientService(TcpForwardTransfer tcpForwardTransfer)
        {
            this.tcpForwardTransfer = tcpForwardTransfer;
        }

        public void AddListen(ClientServiceParamsInfo arg)
        {
            ForwardSettingParamsInfo model = arg.Content.DeJson<ForwardSettingParamsInfo>();

            P2PListenAddParams fmodel = model.Content.DeJson<P2PListenAddParams>();

            string errmsg = tcpForwardTransfer.AddP2PListen(fmodel);
            if (!string.IsNullOrWhiteSpace(errmsg))
            {
                arg.SetCode(ClientServiceResponseCodes.Error, errmsg);
            }
        }
        public void RemoveListen(ClientServiceParamsInfo arg)
        {
            ForwardSettingParamsInfo model = arg.Content.DeJson<ForwardSettingParamsInfo>();
            tcpForwardTransfer.RemoveP2PListen(model.ID);
        }

        public void AddForward(ClientServiceParamsInfo arg)
        {
            ForwardSettingParamsInfo model = arg.Content.DeJson<ForwardSettingParamsInfo>();

            P2PForwardAddParams fmodel = model.Content.DeJson<P2PForwardAddParams>();
            string errmsg = tcpForwardTransfer.AddP2PForward(fmodel);
            if (!string.IsNullOrWhiteSpace(errmsg))
            {
                arg.SetCode(ClientServiceResponseCodes.Error, errmsg);
            }
        }
        public void RemoveForward(ClientServiceParamsInfo arg)
        {
            ForwardSettingParamsInfo model = arg.Content.DeJson<ForwardSettingParamsInfo>();
            P2PForwardRemoveParams fmodel = model.Content.DeJson<P2PForwardRemoveParams>();
            tcpForwardTransfer.RemoveP2PForward(fmodel);
        }

        public IEnumerable<P2PListenInfo> List(ClientServiceParamsInfo arg)
        {
            return tcpForwardTransfer.p2pListens.Where(c => c.ForwardType == common.tcpforward.TcpForwardTypes.FORWARD);
        }
        public IEnumerable<P2PListenInfo> ListProxy(ClientServiceParamsInfo arg)
        {
            return tcpForwardTransfer.p2pListens.Where(c => c.ForwardType == common.tcpforward.TcpForwardTypes.PROXY);
        }
        public string GetPac(ClientServiceParamsInfo arg)
        {
            return tcpForwardTransfer.GetPac();
        }

        public P2PListenInfo Get(ClientServiceParamsInfo arg)
        {
            ForwardSettingParamsInfo model = arg.Content.DeJson<ForwardSettingParamsInfo>();
            return tcpForwardTransfer.GetP2PByID(model.ID);
        }

        public void Start(ClientServiceParamsInfo arg)
        {
            ForwardSettingParamsInfo model = arg.Content.DeJson<ForwardSettingParamsInfo>();
            string errmsg = tcpForwardTransfer.StartP2P(model.ID);
            if (!string.IsNullOrWhiteSpace(errmsg))
            {
                arg.SetCode(ClientServiceResponseCodes.Error, errmsg);
            }
        }
        public void Stop(ClientServiceParamsInfo arg)
        {
            ForwardSettingParamsInfo model = arg.Content.DeJson<ForwardSettingParamsInfo>();
            string errmsg = tcpForwardTransfer.StopP2P(model.ID);
            if (!string.IsNullOrWhiteSpace(errmsg))
            {
                arg.SetCode(ClientServiceResponseCodes.Error, errmsg);
            }
        }


        public List<ServerForwardItemInfo> ServerForwards(ClientServiceParamsInfo arg)
        {
            return tcpForwardTransfer.serverForwards;
        }
        public async Task<int[]> ServerPorts(ClientServiceParamsInfo arg)
        {
            return await tcpForwardTransfer.GetServerPorts();
        }
        public async Task<string> AddServerForward(ClientServiceParamsInfo arg)
        {
            ServerForwardItemInfo forward = arg.Content.DeJson<ServerForwardItemInfo>();
            return await tcpForwardTransfer.AddServerForward(forward);
        }
        public async Task<string> StartServerForward(ClientServiceParamsInfo arg)
        {
            ServerForwardItemInfo forward = arg.Content.DeJson<ServerForwardItemInfo>();
            return await tcpForwardTransfer.StartServerForward(forward);
        }
        public async Task<string> StopServerForward(ClientServiceParamsInfo arg)
        {
            ServerForwardItemInfo forward = arg.Content.DeJson<ServerForwardItemInfo>();
            return await tcpForwardTransfer.StopServerForward(forward);
        }
        public async Task<string> RemoveServerForward(ClientServiceParamsInfo arg)
        {
            ServerForwardItemInfo forward = arg.Content.DeJson<ServerForwardItemInfo>();
            return await tcpForwardTransfer.RemoveServerForward(forward);
        }

    }

    public class ForwardSettingParamsInfo
    {
        public int ID { get; set; } = 0;
        public string Content { get; set; } = string.Empty;
    }
}
