using common.server;
using common.udpforward;

namespace client.service.udpforward
{
    [MessengerIdRange((int)UdpForwardMessengerIds.Min, (int)UdpForwardMessengerIds.Max)]
    public class UdpForwardMessenger : IMessenger
    {
        private readonly UdpForwardMessengerSender udpForwardMessengerSender;
        public UdpForwardMessenger(UdpForwardMessengerSender udpForwardMessengerSender)
        {
            this.udpForwardMessengerSender = udpForwardMessengerSender;
        }

        [MessengerId((int)UdpForwardMessengerIds.Request)]
        public void Request(IConnection connection)
        {
            UdpForwardInfo data = new UdpForwardInfo();
            data.Connection = connection;
            data.DeBytes(connection.ReceiveRequestWrap.Payload);
            udpForwardMessengerSender.OnRequest(data);
        }

        [MessengerId((int)UdpForwardMessengerIds.Response)]
        public void Response(IConnection connection)
        {
            UdpForwardInfo data = new UdpForwardInfo();
            data.Connection = connection;
            data.DeBytes(connection.ReceiveRequestWrap.Payload);
            udpForwardMessengerSender.OnResponse(data);
        }
    }
}
