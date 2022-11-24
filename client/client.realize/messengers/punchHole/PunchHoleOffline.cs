using client.messengers.clients;
using client.messengers.punchHole;

namespace client.realize.messengers.punchHole
{
    public class PunchHoleOffline : IPunchHole
    {
        private readonly IClientInfoCaching clientInfoCaching;
        public PunchHoleOffline(IClientInfoCaching clientInfoCaching)
        {

            this.clientInfoCaching = clientInfoCaching;
        }

        public PunchHoleTypes Type => PunchHoleTypes.OFFLINE;

        public void Execute(OnPunchHoleArg arg)
        {
            clientInfoCaching.Offline(arg.Data.FromId);
        }
    }
}
