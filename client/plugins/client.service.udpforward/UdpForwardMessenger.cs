using common.server;
using common.udpforward;

namespace client.service.udpforward
{
    [MessengerIdRange((ushort)UdpForwardMessengerIds.Min, (ushort)UdpForwardMessengerIds.Max)]
    public class UdpForwardMessenger : IMessenger
    {
        private readonly UdpForwardMessengerSender udpForwardMessengerSender;
        public UdpForwardMessenger(UdpForwardMessengerSender udpForwardMessengerSender)
        {
            this.udpForwardMessengerSender = udpForwardMessengerSender;
        }

        [MessengerId((ushort)UdpForwardMessengerIds.Request)]
        public void Request(IConnection connection)
        {
            UdpForwardInfo data = new UdpForwardInfo();
            data.Connection = connection;
            data.DeBytes(connection.ReceiveRequestWrap.Payload);
            udpForwardMessengerSender.OnRequest(data);
        }

        [MessengerId((ushort)UdpForwardMessengerIds.Response)]
        public void Response(IConnection connection)
        {
            UdpForwardInfo data = new UdpForwardInfo();
            data.Connection = connection;
            data.DeBytes(connection.ReceiveRequestWrap.Payload);
            udpForwardMessengerSender.OnResponse(data);
        }
    }
}
