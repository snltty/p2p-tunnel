using client.messengers.punchHole;

namespace client.realize.messengers.punchHole
{
    /// <summary>
    /// 反向连接
    /// </summary>
    public sealed class PunchHoleReverse : IPunchHole
    {
        private readonly PunchHoleMessengerSender punchHoleMessengerSender;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="punchHoleMessengerSender"></param>
        public PunchHoleReverse(PunchHoleMessengerSender punchHoleMessengerSender)
        {

            this.punchHoleMessengerSender = punchHoleMessengerSender;
        }

        /// <summary>
        /// 
        /// </summary>
        public PunchHoleTypes Type => PunchHoleTypes.REVERSE;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="arg"></param>
        public void Execute(OnPunchHoleArg arg)
        {
            punchHoleMessengerSender.OnReverse.Push(arg);
        }
    }
}
