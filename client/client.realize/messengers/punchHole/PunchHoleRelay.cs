using client.messengers.punchHole;

namespace client.realize.messengers.punchHole
{
    public class PunchHoleRelay : IPunchHole
    {
        private readonly PunchHoleMessengerSender punchHoleMessengerSender;
        public PunchHoleRelay(PunchHoleMessengerSender punchHoleMessengerSender)
        {
            this.punchHoleMessengerSender = punchHoleMessengerSender;
        }

        public PunchHoleTypes Type => PunchHoleTypes.Relay;

        public void Execute(OnPunchHoleArg arg)
        {
            PunchHoleRelayInfo model = new PunchHoleRelayInfo();
            model.DeBytes(arg.Data.Data);

            punchHoleMessengerSender.OnRelay.Push(new OnRelayParam
            {
                Raw = arg,
                Relay = model
            });
        }
    }
}
