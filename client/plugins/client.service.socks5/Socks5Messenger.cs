 using common.server;
using common.socks5;

namespace client.service.socks5
{
    /// <summary>
    /// socks5消息
    /// </summary>
    [MessengerIdRange((ushort)Socks5MessengerIds.Min, (ushort)Socks5MessengerIds.Max)]
    public class Socks5Messenger : IMessenger
    {
        private readonly ISocks5ClientHandler socks5ClientHandler;
        private readonly ISocks5ServerHandler socks5ServerHandler;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="socks5ClientHandler"></param>
        /// <param name="socks5ServerHandler"></param>
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
        public void Request(IConnection connection)
        {
            Socks5Info data = Socks5Info.Debytes(connection.ReceiveRequestWrap.Payload);
            data.Tag = connection;
            data.ClientId = connection.FromConnection.ConnectId;
            socks5ServerHandler.InputData(data);
        }

        /// <summary>
        /// 回执
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)Socks5MessengerIds.Response)]
        public void Response(IConnection connection)
        {
            socks5ClientHandler.InputData(connection);
        }
    }
}
