using client.messengers.clients;
using common.server;

namespace client.realize.messengers
{
    public class SourceConnectionSelector : ISourceConnectionSelector
    {
        private readonly IClientInfoCaching clientInfoCaching;

        public SourceConnectionSelector(IClientInfoCaching clientInfoCaching)
        {
            this.clientInfoCaching = clientInfoCaching;
        }

        public IConnection Select(IConnection connection)
        {
            if (connection.ReceiveRequestWrap.Relay == 1)
            {
                if (clientInfoCaching.Get(connection.ReceiveRequestWrap.RelayId, out ClientInfo client))
                {
                    return connection.ServerType == common.server.model.ServerType.TCP ? client.TcpConnection : client.UdpConnection;
                }
            }
            return connection;
        }
    }
}
