using common.server;
using common.tcpforward;

namespace client.service.tcpforward
{
    public class TcpForwardMessenger : IMessenger
    {
        private readonly TcpForwardResolver tcpForwardResolver;
        private readonly ITcpForwardServer tcpForwardServer;

        public TcpForwardMessenger(TcpForwardResolver tcpForwardResolver, ITcpForwardServer tcpForwardServer)
        {
            this.tcpForwardResolver = tcpForwardResolver;
            this.tcpForwardServer = tcpForwardServer;
        }

        public void Request(IConnection connection)
        {
            tcpForwardResolver.InputData(connection);
        }

        public void Response(IConnection connection)
        {
            TcpForwardInfo data = new TcpForwardInfo();
            data.Connection = connection;
            data.DeBytes(connection.ReceiveRequestWrap.Memory);
            tcpForwardServer.Response(data);
        }
    }
}
