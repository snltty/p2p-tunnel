using client.messengers.singnin;
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
        public ClientsMessenger(ClientsMessengerSender clientsMessengerSender)
        {
            this.clientsMessengerSender = clientsMessengerSender;
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
    }
}
