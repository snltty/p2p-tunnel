using client.service.ui.api.clientServer;
using common.libs.extends;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace client.service.udpforward
{
    public class UdpForwardClientService : IClientService
    {
        private readonly UdpForwardTransfer udpForwardTransfer;
        public UdpForwardClientService(UdpForwardTransfer udpForwardTransfer)
        {
            this.udpForwardTransfer = udpForwardTransfer;
        }

        public void AddListen(ClientServiceParamsInfo arg)
        {
            ForwardSettingParamsInfo model = arg.Content.DeJson<ForwardSettingParamsInfo>();

            P2PListenInfo fmodel = model.Content.DeJson<P2PListenInfo>();

            string errmsg = udpForwardTransfer.AddP2PListen(fmodel);
            if (!string.IsNullOrWhiteSpace(errmsg))
            {
                arg.SetCode(ClientServiceResponseCodes.Error, errmsg);
            }
        }
        public void RemoveListen(ClientServiceParamsInfo arg)
        {
            ForwardSettingParamsInfo model = arg.Content.DeJson<ForwardSettingParamsInfo>();
            udpForwardTransfer.RemoveP2PListen(model.Port);
        }

        public IEnumerable<P2PListenInfo> List(ClientServiceParamsInfo arg)
        {
            return udpForwardTransfer.p2PConfigInfo.Tunnels;
        }
        public P2PListenInfo Get(ClientServiceParamsInfo arg)
        {
            ForwardSettingParamsInfo model = arg.Content.DeJson<ForwardSettingParamsInfo>();
            return udpForwardTransfer.GetP2PByPort(model.Port);
        }

        public void Start(ClientServiceParamsInfo arg)
        {
            ForwardSettingParamsInfo model = arg.Content.DeJson<ForwardSettingParamsInfo>();
            string errmsg = udpForwardTransfer.StartP2P(model.Port);
            if (!string.IsNullOrWhiteSpace(errmsg))
            {
                arg.SetCode(ClientServiceResponseCodes.Error, errmsg);
            }
        }
        public void Stop(ClientServiceParamsInfo arg)
        {
            ForwardSettingParamsInfo model = arg.Content.DeJson<ForwardSettingParamsInfo>();
            udpForwardTransfer.StopP2PListen(model.Port);
        }

        public List<ServerForwardItemInfo> ServerForwards(ClientServiceParamsInfo arg)
        {
            return udpForwardTransfer.serverConfigInfo.Tunnels;
        }
        public async Task<int[]> ServerPorts(ClientServiceParamsInfo arg)
        {
            return await udpForwardTransfer.GetServerPorts();
        }
        public async Task<string> AddServerForward(ClientServiceParamsInfo arg)
        {
            ServerForwardItemInfo forward = arg.Content.DeJson<ServerForwardItemInfo>();
            return await udpForwardTransfer.AddServerForward(forward);
        }
        public async Task<string> StartServerForward(ClientServiceParamsInfo arg)
        {
            ForwardSettingParamsInfo forward = arg.Content.DeJson<ForwardSettingParamsInfo>();
            return await udpForwardTransfer.StartServerForward(forward.Port);
        }
        public async Task<string> StopServerForward(ClientServiceParamsInfo arg)
        {
            ForwardSettingParamsInfo forward = arg.Content.DeJson<ForwardSettingParamsInfo>();
            return await udpForwardTransfer.StopServerForward(forward.Port);
        }
        public async Task<string> RemoveServerForward(ClientServiceParamsInfo arg)
        {
            ForwardSettingParamsInfo forward = arg.Content.DeJson<ForwardSettingParamsInfo>();
            return await udpForwardTransfer.RemoveServerForward(forward.Port);
        }
    }

    public class ForwardSettingParamsInfo
    {
        public int Port { get; set; } = 0;
        public string Content { get; set; } = string.Empty;
    }
}
