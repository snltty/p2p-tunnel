using client.messengers.register;
using common.libs.extends;
using common.server;
using common.server.model;
using System.Threading.Tasks;

namespace client.service.messengers.clients
{
    /// <summary>
    /// 服务器发来的客户端列表
    /// </summary>
    public class ClientsMessenger : IMessenger
    {
        private readonly ClientsMessengerSender clientsMessengerSender;
        private readonly IRegisterTransfer registerTransfer;
        public ClientsMessenger(ClientsMessengerSender clientsMessengerSender, IRegisterTransfer registerTransfer)
        {
            this.clientsMessengerSender = clientsMessengerSender;
            this.registerTransfer = registerTransfer;
        }

        public void Execute(IConnection connection)
        {
            ClientsInfo res = connection.ReceiveRequestWrap.Memory.DeBytes<ClientsInfo>();
            clientsMessengerSender.OnServerClientsData.Push(res);
        }

        public async Task Reset(IConnection connection)
        {
            await registerTransfer.Register().ConfigureAwait(false);
        }
    }
}
