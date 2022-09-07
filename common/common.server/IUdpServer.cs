using System.Net;
using System.Threading.Tasks;

namespace common.server
{
    public interface IUdpServer : IServer
    {
        public Task<IConnection> CreateConnection(IPEndPoint address);
    }
}
