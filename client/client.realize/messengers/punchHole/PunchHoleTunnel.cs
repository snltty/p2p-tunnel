using client.messengers.punchHole;

namespace client.realize.messengers.punchHole
{
    public sealed class PunchHoleTunnel : IPunchHole
    {
        private readonly PunchHoleMessengerSender punchHoleMessengerSender;
        public PunchHoleTunnel(PunchHoleMessengerSender punchHoleMessengerSender)
        {
            this.punchHoleMessengerSender = punchHoleMessengerSender;
        }

        public PunchHoleTypes Type => PunchHoleTypes.TUNNEL;

        public void Execute(OnPunchHoleArg arg)
        {
            PunchHoleTunnelInfo info = new PunchHoleTunnelInfo();
            info.DeBytes(arg.Data.Data);
            punchHoleMessengerSender.OnTunnel.Push(info);
        }
    }
}
