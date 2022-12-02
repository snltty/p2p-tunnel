using client.messengers.punchHole;

namespace client.realize.messengers.punchHole
{
    public sealed class PunchHoleReverse : IPunchHole
    {
        private readonly PunchHoleMessengerSender punchHoleMessengerSender;
        public PunchHoleReverse(PunchHoleMessengerSender punchHoleMessengerSender)
        {

            this.punchHoleMessengerSender = punchHoleMessengerSender;
        }

        public PunchHoleTypes Type => PunchHoleTypes.REVERSE;

        public void Execute(OnPunchHoleArg arg)
        {
            punchHoleMessengerSender.OnReverse.Push(arg);
        }
    }
}
