using common.server;
using common.tcpforward;

namespace client.service.tcpforward
{
    public class TcpForwardMessenger : IMessenger
    {
        private readonly TcpForwardMessengerSender tcpForwardMessengerSender;
        public TcpForwardMessenger(TcpForwardMessengerSender tcpForwardMessengerSender)
        {
            this.tcpForwardMessengerSender = tcpForwardMessengerSender;
        }

        public void Request(IConnection connection)
        {
            TcpForwardInfo data = new TcpForwardInfo();
            data.Connection = connection;
            data.DeBytes(connection.ReceiveRequestWrap.Memory);
            tcpForwardMessengerSender.OnRequest(data);
        }

        public void Response(IConnection connection)
        {
            TcpForwardInfo data = new TcpForwardInfo();
            data.Connection = connection;
            data.DeBytes(connection.ReceiveRequestWrap.Memory);
            tcpForwardMessengerSender.OnResponse(data);
        }
    }
}
