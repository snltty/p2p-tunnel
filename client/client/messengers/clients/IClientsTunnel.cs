using common.server;
using common.server.model;
using System;
using System.Threading.Tasks;

namespace client.messengers.clients
{
    public interface IClientsTunnel
    {
        public Action<IConnection, IConnection> OnDisConnect { get; set; }
        public Task<(ulong, int)> NewBind(ServerType serverType, ulong tunnelName);
    }
}
