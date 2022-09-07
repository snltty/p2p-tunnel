using common.server;
using common.udpforward;

namespace client.service.udpforward
{
    public class UdpForwardMessenger : IMessenger
    {
        private readonly UdpForwardMessengerSender udpForwardMessengerSender;
        public UdpForwardMessenger(UdpForwardMessengerSender udpForwardMessengerSender)
        {
            this.udpForwardMessengerSender = udpForwardMessengerSender;
        }

        public void Request(IConnection connection)
        {
            UdpForwardInfo data = new UdpForwardInfo();
            data.Connection = connection;
            data.DeBytes(connection.ReceiveRequestWrap.Memory);
            udpForwardMessengerSender.OnRequest(data);
        }

        public void Response(IConnection connection)
        {
            UdpForwardInfo data = new UdpForwardInfo();
            data.Connection = connection;
            data.DeBytes(connection.ReceiveRequestWrap.Memory);
            udpForwardMessengerSender.OnResponse(data);
        }
    }
}
