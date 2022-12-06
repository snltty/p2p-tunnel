using client.messengers.clients;
using client.messengers.punchHole;
using common.libs;

namespace client.realize.messengers.punchHole
{
    /// <summary>
    /// 掉线
    /// </summary>
    public sealed class PunchHoleOffline : IPunchHole
    {
        private readonly IClientInfoCaching clientInfoCaching;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientInfoCaching"></param>
        public PunchHoleOffline(IClientInfoCaching clientInfoCaching)
        {

            this.clientInfoCaching = clientInfoCaching;
        }

        /// <summary>
        /// 
        /// </summary>
        public PunchHoleTypes Type => PunchHoleTypes.OFFLINE;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="arg"></param>
        public void Execute(OnPunchHoleArg arg)
        {
            if (clientInfoCaching.Get(arg.Data.FromId, out ClientInfo client))
            {
                Logger.Instance.Error($"{client.Name},offline:4");
            }

            clientInfoCaching.Offline(arg.Data.FromId);
        }
    }
}
