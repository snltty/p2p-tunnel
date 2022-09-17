using common.server.model;
using System.Threading.Tasks;

namespace client.messengers.clients
{
    public interface IClientsTunnel
    {
        public Task<(ulong, int)> NewBind(ServerType serverType, ulong tunnelName);
    }
}
