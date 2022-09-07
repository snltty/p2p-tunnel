using common.libs;
using common.server.model;

namespace client.realize.messengers.clients
{
    public class ClientsMessengerSender
    {
        public ClientsMessengerSender()
        {
        }

        public SimpleSubPushHandler<ClientsInfo> OnServerClientsData { get; } = new SimpleSubPushHandler<ClientsInfo>();

    }
}
