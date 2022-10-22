using common.server;
using common.server.model;

namespace client.messengers.clients
{
    public interface IRelayConnectionSelector
    {
        public IConnection Select(ServerType serverType);
    }
}
