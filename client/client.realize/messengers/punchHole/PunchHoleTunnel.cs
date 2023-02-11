using client.messengers.punchHole;
using common.server;
using common.server.model;
using System.Threading.Tasks;

namespace client.realize.messengers.punchHole
{
    /// <summary>
    /// 收到新通道消息
    /// </summary>
    public sealed class PunchHoleTunnel : IPunchHole
    {
        private readonly PunchHoleMessengerSender punchHoleMessengerSender;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="punchHoleMessengerSender"></param>
        public PunchHoleTunnel(PunchHoleMessengerSender punchHoleMessengerSender)
        {
            this.punchHoleMessengerSender = punchHoleMessengerSender;
        }

        /// <summary>
        /// 
        /// </summary>
        public PunchHoleTypes Type => PunchHoleTypes.TUNNEL;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="arg"></param>
        public async Task Execute(IConnection connection, PunchHoleRequestInfo info)
        {
            PunchHoleTunnelInfo tunnelinfo = new PunchHoleTunnelInfo();
            tunnelinfo.DeBytes(info.Data);
            punchHoleMessengerSender.OnTunnel.Push(tunnelinfo);
            await Task.CompletedTask;
        }
    }
}
