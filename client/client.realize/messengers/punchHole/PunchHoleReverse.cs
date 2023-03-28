using client.messengers.punchHole;
using common.server;
using common.server.model;
using System.Threading.Tasks;

namespace client.realize.messengers.punchHole
{
    /// <summary>
    /// 反向连接
    /// </summary>
    public sealed class PunchHoleReverse : IPunchHole
    {
        private readonly PunchHoleMessengerSender punchHoleMessengerSender;
        public PunchHoleReverse(PunchHoleMessengerSender punchHoleMessengerSender)
        {

            this.punchHoleMessengerSender = punchHoleMessengerSender;
        }

        public PunchHoleTypes Type => PunchHoleTypes.REVERSE;
        public async Task Execute(IConnection connection, PunchHoleRequestInfo info)
        {
            punchHoleMessengerSender.OnReverse?.Invoke(info);
            await Task.CompletedTask;
        }
    }
}
