using client.messengers.clients;
using client.messengers.register;
using client.service.ui.api.clientServer;
using common.libs.extends;
using common.server;
using common.server.model;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace client.service.ui.api.service.clientServer.services
{
    public class ClientsClientService : IClientService
    {
        private readonly IClientsTransfer clientsTransfer;
        private readonly IClientInfoCaching clientInfoCaching;
        private readonly RegisterStateInfo registerStateInfo;

        public ClientsClientService(IClientsTransfer clientsTransfer, IClientInfoCaching clientInfoCaching, RegisterStateInfo registerStateInfo)
        {
            this.clientsTransfer = clientsTransfer;
            this.clientInfoCaching = clientInfoCaching;
            this.registerStateInfo = registerStateInfo;
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

        public void Offline(ClientServiceParamsInfo arg)
        {
            ConnectParamsInfo model = arg.Content.DeJson<ConnectParamsInfo>();
            clientInfoCaching.Offline(model.ID, model.Type);
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

        public async Task<bool> Ping(ClientServiceParamsInfo arg)
        {
            await clientsTransfer.Ping();
            return true;
        }

        public async Task<ConcurrentDictionary<ulong, ConnectInfo[]>> Connects(ClientServiceParamsInfo arg)
        {
            return await clientsTransfer.Connects();
        }
        public async Task<bool> Routes(ClientServiceParamsInfo arg)
        {
            RoutesInfo routes = arg.Content.DeJson<RoutesInfo>();
            return await clientsTransfer.Routes(routes);
        }
        public async Task<Dictionary<ulong, int[]>> Delay(ClientServiceParamsInfo arg)
        {
            return await clientsTransfer.Delay(ulong.Parse(arg.Content));
        }
        public async Task<bool> Relay(ClientServiceParamsInfo arg)
        {
            RelayParamsInfo model = arg.Content.DeJson<RelayParamsInfo>();

            IConnection sourceConnection = null;
            if (model.ID == 0)
            {
                sourceConnection = model.Type == ServerType.TCP ? registerStateInfo.TcpConnection : registerStateInfo.UdpConnection;
            }
            else if (clientInfoCaching.Get(model.ID, out ClientInfo sourceClient))
            {
                sourceConnection = model.Type == ServerType.TCP ? sourceClient.TcpConnection : sourceClient.UdpConnection;
            }

            if (sourceConnection != null && sourceConnection.Connected && clientInfoCaching.Get(model.ToId, out ClientInfo targetClient))
            {
                await clientsTransfer.Relay(targetClient, sourceConnection, true);
            }

            return true;
        }

    }
    public class ConnectParamsInfo
    {
        public ulong ID { get; set; } = 0;
        public ServerType Type { get; set; } = ServerType.TCP;
    }
    public class RelayParamsInfo
    {
        public ulong ID { get; set; } = 0;
        public ulong ToId { get; set; } = 0;
        public ServerType Type { get; set; } = ServerType.TCP;
    }
}
