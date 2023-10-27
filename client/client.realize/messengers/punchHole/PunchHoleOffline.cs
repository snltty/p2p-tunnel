using client.messengers.clients;
using client.messengers.punchHole;
using common.libs;
using common.server;
using common.server.model;
using System.Threading.Tasks;

namespace client.realize.messengers.punchHole
{
    /// <summary>
    /// 掉线
    /// </summary>
    public sealed class PunchHoleOffline : IPunchHole
    {
        private readonly IClientInfoCaching clientInfoCaching;
        public PunchHoleOffline(IClientInfoCaching clientInfoCaching)
        {

            this.clientInfoCaching = clientInfoCaching;
        }

        public PunchHoleTypes Type => PunchHoleTypes.OFFLINE;

        public async Task Execute(IConnection connection, PunchHoleRequestInfo info)
        {
            clientInfoCaching.Offline(info.FromId, ClientOfflineTypes.Manual);
            await Task.CompletedTask;
        }
    }
}
