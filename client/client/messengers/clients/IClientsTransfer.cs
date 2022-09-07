using common.server;
using System.Threading.Tasks;

namespace client.messengers.clients
{
    public interface IClientsTransfer
    {
        public void ConnectClient(ulong id);
        public void ConnectClient(ClientInfo info);
        public void ConnectReverse(ulong id);
        public void Reset(ulong id);
        public void ConnectStop(ulong id);
    }
}
