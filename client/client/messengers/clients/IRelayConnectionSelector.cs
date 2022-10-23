using common.server;
using common.server.model;
using System.Threading.Tasks;

namespace client.messengers.clients
{
    public interface IRelayConnectionSelector
    {
        public Task<IConnection> Select(ServerType serverType);
    }
}
