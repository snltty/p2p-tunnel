using common.server;
using common.socks5;
using System.Threading.Tasks;

namespace client.service.socks5
{
    /// <summary>
    /// socks5消息
    /// </summary>
    [MessengerIdRange((ushort)Socks5MessengerIds.Min, (ushort)Socks5MessengerIds.Max)]
    public sealed class Socks5Messenger : IMessenger
    {
        private readonly ISocks5ClientHandler socks5ClientHandler;
        private readonly ISocks5ServerHandler socks5ServerHandler;
        public Socks5Messenger(ISocks5ClientHandler socks5ClientHandler, ISocks5ServerHandler socks5ServerHandler)
        {
            this.socks5ClientHandler = socks5ClientHandler;
            this.socks5ServerHandler = socks5ServerHandler;
        }

        /// <summary>
        /// 请求
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)Socks5MessengerIds.Request)]
        public async Task Request(IConnection connection)
        {
            Socks5Info data = Socks5Info.Debytes(connection.ReceiveRequestWrap.Payload);
            data.Tag = connection;
            data.ClientId = connection.FromConnection.ConnectId;
            await socks5ServerHandler.InputData(data);
        }

        /// <summary>
        /// 回执
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)Socks5MessengerIds.Response)]
        public async Task Response(IConnection connection)
        {
            Socks5Info info = Socks5Info.Debytes(connection.ReceiveRequestWrap.Payload);
            await socks5ClientHandler.InputData(info);
        }
    }
}
