using client.messengers.punchHole;
using client.messengers.register;

namespace client.realize.messengers.punchHole
{
    /// <summary>
    /// 重启
    /// </summary>
    public sealed class PunchHoleReset : IPunchHole
    {
        private readonly IRegisterTransfer registerTransfer;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="registerTransfer"></param>
        public PunchHoleReset(IRegisterTransfer registerTransfer)
        {

            this.registerTransfer = registerTransfer;
        }

        /// <summary>
        /// 
        /// </summary>
        public PunchHoleTypes Type => PunchHoleTypes.RESET;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="arg"></param>
        public void Execute(OnPunchHoleArg arg)
        {
            _ = registerTransfer.Register(true);
        }
    }
}
