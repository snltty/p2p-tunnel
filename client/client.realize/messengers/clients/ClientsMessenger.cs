using client.messengers.signin;
using client.realize.messengers.signin;
using common.server;
using common.server.model;

namespace client.realize.messengers.clients
{
    /// <summary>
    /// 服务器发来的客户端列表
    /// </summary>
    [MessengerIdRange((ushort)ClientsMessengerIds.Min, (ushort)ClientsMessengerIds.Max)]
    public sealed class ClientsMessenger : IMessenger
    {
        private readonly ClientsMessengerSender clientsMessengerSender;
        private readonly ISignInTransfer signInTransfer;

        public ClientsMessenger(ClientsMessengerSender clientsMessengerSender, ISignInTransfer signInTransfer)
        {
            this.clientsMessengerSender = clientsMessengerSender;
            this.signInTransfer = signInTransfer;
        }

        /// <summary>
        /// 通知信息
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)ClientsMessengerIds.Notify)]
        public void Notify(IConnection connection)
        {
            ClientsInfo res = new ClientsInfo();
            res.DeBytes(connection.ReceiveRequestWrap.Payload);
            clientsMessengerSender.OnServerClientsData?.Invoke(res);
        }

        /// <summary>
        /// 退出信息
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)ClientsMessengerIds.Exit)]
        public void Exit(IConnection connection)
        {
            signInTransfer.Exit();
        }
    }
}
