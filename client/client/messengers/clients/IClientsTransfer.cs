using common.server;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace client.messengers.clients
{
    public interface IClientsTransfer
    {
        public void ConnectClient(ulong id);
        public void ConnectClient(ClientInfo info);
        public void ConnectReverse(ClientInfo info);
        public void ConnectReverse(ulong id);
        public void Reset(ulong id);
        public void ConnectStop(ulong id);
        public Task Ping();

        public Task<ConcurrentDictionary<ulong, ulong[]>> Connects();
        public Task<int[]> Delay(ulong[][] paths);
        public Task Relay(IConnection sourceConnection, ulong[] relayids, bool notify = false);
    }
}
