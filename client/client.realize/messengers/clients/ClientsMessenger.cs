using client.messengers.register;
using common.server;
using common.server.model;
using System.Threading.Tasks;

namespace client.realize.messengers.clients
{
    /// <summary>
    /// 服务器发来的客户端列表
    /// </summary>
    [MessengerIdRange((ushort)ClientsMessengerIds.Min, (ushort)ClientsMessengerIds.Max)]
    public sealed class ClientsMessenger : IMessenger
    {
        private readonly ClientsMessengerSender clientsMessengerSender;
        private readonly IRegisterTransfer registerTransfer;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientsMessengerSender"></param>
        /// <param name="registerTransfer"></param>
        public ClientsMessenger(ClientsMessengerSender clientsMessengerSender, IRegisterTransfer registerTransfer)
        {
            this.clientsMessengerSender = clientsMessengerSender;
            this.registerTransfer = registerTransfer;
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
            clientsMessengerSender.OnServerClientsData.Push(res);
        }
    }
}
