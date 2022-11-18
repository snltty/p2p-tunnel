using client.messengers.clients;
using client.messengers.register;
using client.service.ui.api.clientServer;
using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using System;
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
            if (clientInfoCaching.Get(model.ID, out ClientInfo client) == false)
            {
                return;
            }
            clientInfoCaching.Offline(model.ID);
            clientsTransfer.ConnectClient(model.ID);
        }

        public void ConnectReverse(ClientServiceParamsInfo arg)
        {
            ConnectParamsInfo model = arg.Content.DeJson<ConnectParamsInfo>();
            if (clientInfoCaching.Get(model.ID, out ClientInfo client) == false)
            {
                return;
            }
            clientInfoCaching.Offline(model.ID);
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

        public async Task<ConcurrentDictionary<ulong, ulong[]>> Connects(ClientServiceParamsInfo arg)
        {
            return await clientsTransfer.Connects();
        }
        public async Task<int[]> Delay(ClientServiceParamsInfo arg)
        {
            return await clientsTransfer.Delay(arg.Content.DeJson<ulong[][]>());
        }
        public async Task<bool> Relay(ClientServiceParamsInfo arg)
        {
            ulong[] relayids = arg.Content.DeJson<ulong[]>();
            if (relayids.Length < 3) return false;

            ulong connectId = relayids[1];
            ulong targetId = relayids[^1];

            IConnection sourceConnection = null;
            if (connectId == 0)
            {
                sourceConnection = registerStateInfo.OnlineConnection;
            }
            else if (clientInfoCaching.Get(connectId, out ClientInfo sourceClient))
            {
                sourceConnection = sourceClient.Connection;
            }
            if (sourceConnection == null)
            {
                return false;
            }

            clientInfoCaching.Offline(targetId);
            await clientsTransfer.Relay(sourceConnection, relayids, true);

            return true;
        }

    }
    public class ConnectParamsInfo
    {
        public ulong ID { get; set; } = 0;
    }
    public class RelayParamsInfo
    {
        public ulong[] RelayIds { get; set; } = Helper.EmptyUlongArray;
        public ulong ToId { get; set; } = 0;
    }
}
