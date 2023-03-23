using common.libs;
using System.Threading.Tasks;

namespace common.udpforward
{
    /// <summary>
    /// 
    /// </summary>
    public class UdpForwardTransferBase
    {
        private readonly IUdpForwardServer udpForwardServer;
        private readonly UdpForwardMessengerSender udpForwardMessengerSender;
        private readonly IUdpForwardTargetProvider udpForwardTargetProvider;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="udpForwardServer"></param>
        /// <param name="udpForwardMessengerSender"></param>
        /// <param name="udpForwardTargetProvider"></param>
        public UdpForwardTransferBase(IUdpForwardServer udpForwardServer, UdpForwardMessengerSender udpForwardMessengerSender, IUdpForwardTargetProvider udpForwardTargetProvider)
        {
            this.udpForwardServer = udpForwardServer;
            this.udpForwardMessengerSender = udpForwardMessengerSender;
            this.udpForwardTargetProvider = udpForwardTargetProvider;

            //A来了请求 ，转发到B，
            udpForwardServer.OnRequest = OnRequest;
            //A收到B的回复
            udpForwardMessengerSender.OnResponseHandle = udpForwardServer.Response;
        }


        private async Task OnRequest(UdpForwardInfo request)
        {
            if (request.Connection == null || !request.Connection.Connected)
            {
                request.Connection = null;
                udpForwardTargetProvider?.Get(request.SourcePort, request);
            }

            if (request.Connection == null)
            {
                request.Buffer = Helper.EmptyArray;
                await udpForwardServer.Response(request);
            }
            else
            {
                request.Connection = request.Connection;
                await udpForwardMessengerSender.SendRequest(request);
            }
        }
    }
}
