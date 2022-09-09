using client.messengers.punchHole;
using client.messengers.register;

namespace client.realize.messengers.punchHole
{
    public class PunchHoleTunnel : IPunchHole
    {
        private readonly PunchHoleMessengerSender punchHoleMessengerSender;
        public PunchHoleTunnel(PunchHoleMessengerSender punchHoleMessengerSender)
        {
            this.punchHoleMessengerSender = punchHoleMessengerSender;
        }

        public PunchHoleTypes Type => PunchHoleTypes.Tunnel;

        public void Execute(OnPunchHoleArg arg)
        {
            PunchHoleTunnelInfo info = new PunchHoleTunnelInfo();
            info.DeBytes(arg.Data.Data);
            punchHoleMessengerSender.OnTunnel.Push(info);
        }
    }
}
