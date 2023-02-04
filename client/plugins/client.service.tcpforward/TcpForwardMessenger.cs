using common.server;
using common.tcpforward;
using System.Threading.Tasks;

namespace client.service.tcpforward
{
    /// <summary>
    /// tcp转发消息
    /// </summary>
    [MessengerIdRange((ushort)TcpForwardMessengerIds.Min, (ushort)TcpForwardMessengerIds.Max)]
    public sealed class TcpForwardMessenger : IMessenger
    {
        private readonly TcpForwardResolver tcpForwardResolver;
        private readonly ITcpForwardServer tcpForwardServer;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tcpForwardResolver"></param>
        /// <param name="tcpForwardServer"></param>
        public TcpForwardMessenger(TcpForwardResolver tcpForwardResolver, ITcpForwardServer tcpForwardServer)
        {
            this.tcpForwardResolver = tcpForwardResolver;
            this.tcpForwardServer = tcpForwardServer;
        }

        /// <summary>
        /// 请求
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)TcpForwardMessengerIds.Request)]
        public void Request(IConnection connection)
        {
            tcpForwardResolver.InputData(connection);
        }

        /// <summary>
        /// 回复
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)TcpForwardMessengerIds.Response)]
        public async Task Response(IConnection connection)
        {
            TcpForwardInfo data = new TcpForwardInfo();
            data.Connection = connection;
            data.DeBytes(connection.ReceiveRequestWrap.Payload);
            await tcpForwardServer.Response(data);
        }
    }
}
