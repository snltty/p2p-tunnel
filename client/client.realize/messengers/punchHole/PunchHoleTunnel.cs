using client.messengers.punchHole;

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
        public void Execute(OnPunchHoleArg arg)
        {
            PunchHoleTunnelInfo info = new PunchHoleTunnelInfo();
            info.DeBytes(arg.Data.Data);
            punchHoleMessengerSender.OnTunnel.Push(info);
        }
    }
}
