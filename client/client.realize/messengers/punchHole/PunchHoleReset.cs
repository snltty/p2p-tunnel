using client.messengers.punchHole;
using client.messengers.register;
using System;

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
            Console.WriteLine($"reset");
            _ = registerTransfer.Register(true);
        }
    }
}
