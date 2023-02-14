using client.service.ui.api.clientServer;
using common.libs.extends;
using System.Linq;

namespace client.service.tcpforward
{
    /// <summary>
    /// http代理
    /// </summary>
    public sealed class HttpProxyClientService : IClientService
    {
        private readonly TcpForwardTransfer tcpForwardTransfer;
        public HttpProxyClientService(TcpForwardTransfer tcpForwardTransfer)
        {
            this.tcpForwardTransfer = tcpForwardTransfer;
        }
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
       
        public P2PListenInfo ListProxy(ClientServiceParamsInfo arg)
        {
            return tcpForwardTransfer.p2pListens.FirstOrDefault(c => c.ForwardType == common.tcpforward.TcpForwardTypes.Proxy) ?? new P2PListenInfo
            {
                ID = 0,
                Port = 5412,
                AliveType = common.tcpforward.TcpForwardAliveTypes.Web,
                ForwardType = common.tcpforward.TcpForwardTypes.Proxy,
                Listening = false,
                IsPac = false,
                IsCustomPac = false,
                Name = string.Empty,
                Desc = string.Empty,
            };
        }
        public string GetPac(ClientServiceParamsInfo arg)
        {
            return tcpForwardTransfer.GetPac();
        }
    }
}
