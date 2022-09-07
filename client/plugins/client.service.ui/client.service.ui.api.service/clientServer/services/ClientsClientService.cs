using client.messengers.clients;
using client.service.ui.api.clientServer;
using common.libs.extends;
using System.Collections.Generic;

namespace client.service.ui.api.service.clientServer.services
{
    public class ClientsClientService : IClientService
    {
        private readonly IClientsTransfer clientsTransfer;
        private readonly IClientInfoCaching clientInfoCaching;

        public ClientsClientService(IClientsTransfer clientsTransfer, IClientInfoCaching clientInfoCaching)
        {
            this.clientsTransfer = clientsTransfer;
            this.clientInfoCaching = clientInfoCaching;
        }

        public IEnumerable<ClientInfo> List(ClientServiceParamsInfo arg)
        {
            return clientInfoCaching.All();
        }

        public void Connect(ClientServiceParamsInfo arg)
        {
            ConnectParamsInfo model = arg.Content.DeJson<ConnectParamsInfo>();
            clientsTransfer.ConnectClient(model.ID);
        }

        public void Stop(ClientServiceParamsInfo arg)
        {
            ConnectParamsInfo model = arg.Content.DeJson<ConnectParamsInfo>();
            clientsTransfer.ConnectStop(model.ID);
        }

        public void Offline(ClientServiceParamsInfo arg)
        {
            ConnectParamsInfo model = arg.Content.DeJson<ConnectParamsInfo>();
            clientInfoCaching.Offline(model.ID);
        }

        public void ConnectReverse(ClientServiceParamsInfo arg)
        {
            ConnectParamsInfo model = arg.Content.DeJson<ConnectParamsInfo>();
            clientsTransfer.ConnectReverse(model.ID);
        }

        public void Reset(ClientServiceParamsInfo arg)
        {
            ConnectParamsInfo model = arg.Content.DeJson<ConnectParamsInfo>();
            clientsTransfer.Reset(model.ID);
        }

    }
    public class ConnectParamsInfo
    {
        public ulong ID { get; set; } = 0;
    }
}
