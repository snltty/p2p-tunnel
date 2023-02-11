using client.messengers.punchHole;
using client.messengers.register;
using common.server;
using common.server.model;
using System.Threading.Tasks;

namespace client.realize.messengers.punchHole
{
    /// <summary>
    /// 重启
    /// </summary>
    public sealed class PunchHoleReset : IPunchHole
    {
        private readonly IRegisterTransfer registerTransfer;
        public PunchHoleReset(IRegisterTransfer registerTransfer)
        {

            this.registerTransfer = registerTransfer;
        }
        public PunchHoleTypes Type => PunchHoleTypes.RESET;
        public async Task Execute(IConnection connection, PunchHoleRequestInfo info)
        {
            _ = registerTransfer.Register(true);
            await Task.CompletedTask;
        }
    }
}
