using common.libs;
using common.libs.extends;
using System;
using System.Text;

namespace common.udpforward
{
    public class UdpForwardTransferBase
    {
        private readonly IUdpForwardServer udpForwardServer;
        private readonly UdpForwardMessengerSender udpForwardMessengerSender;
        private readonly IUdpForwardTargetProvider udpForwardTargetProvider;


        public UdpForwardTransferBase(IUdpForwardServer udpForwardServer, UdpForwardMessengerSender udpForwardMessengerSender, IUdpForwardTargetProvider udpForwardTargetProvider)
        {
            this.udpForwardServer = udpForwardServer;
            this.udpForwardMessengerSender = udpForwardMessengerSender;
            this.udpForwardTargetProvider = udpForwardTargetProvider;

            //A来了请求 ，转发到B，
            udpForwardServer.OnRequest.Sub(OnRequest);
            //A收到B的回复
            udpForwardMessengerSender.OnResponseHandler.Sub(udpForwardServer.Response);
        }

        private void OnRequest(UdpForwardInfo request)
        {
            if (request.Connection == null || !request.Connection.Connected)
            {
                request.Connection = null;
                udpForwardTargetProvider?.Get(request.SourcePort, request);
            }

            if (request.Connection == null)
            {
                request.Buffer = Helper.EmptyArray;
                udpForwardServer.Response(request);
            }
            else
            {
                request.Connection = request.Connection;
                request.Connection.ReceiveBytes += request.Buffer.Length;
                _ = udpForwardMessengerSender.SendRequest(request);
            }
        }
    }
}
