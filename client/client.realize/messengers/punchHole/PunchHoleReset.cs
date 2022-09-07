using client.messengers.punchHole;
using client.messengers.register;

namespace client.realize.messengers.punchHole
{
    public class PunchHoleReset : IPunchHole
    {
        private readonly IRegisterTransfer registerTransfer;
        public PunchHoleReset(IRegisterTransfer registerTransfer)
        {

            this.registerTransfer = registerTransfer;
        }

        public PunchHoleTypes Type => PunchHoleTypes.RESET;

        public void Execute(OnPunchHoleArg arg)
        {
            _ = registerTransfer.Register();
        }
    }
}
