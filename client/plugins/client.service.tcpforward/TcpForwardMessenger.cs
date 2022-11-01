using common.server;
using common.tcpforward;

namespace client.service.tcpforward
{
    [MessengerIdRange((int)TcpForwardMessengerIds.Min, (int)TcpForwardMessengerIds.Max)]
    public class TcpForwardMessenger : IMessenger
    {
        private readonly TcpForwardResolver tcpForwardResolver;
        private readonly ITcpForwardServer tcpForwardServer;

        public TcpForwardMessenger(TcpForwardResolver tcpForwardResolver, ITcpForwardServer tcpForwardServer)
        {
            this.tcpForwardResolver = tcpForwardResolver;
            this.tcpForwardServer = tcpForwardServer;
        }

        [MessengerId((int)TcpForwardMessengerIds.Request)]
        public void Request(IConnection connection)
        {
            tcpForwardResolver.InputData(connection);
        }

        [MessengerId((int)TcpForwardMessengerIds.Response)]
        public void Response(IConnection connection)
        {
            TcpForwardInfo data = new TcpForwardInfo();
            data.Connection = connection;
            data.DeBytes(connection.ReceiveRequestWrap.Payload);
            tcpForwardServer.Response(data);
        }
    }
}
