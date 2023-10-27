using client.messengers.punchHole;
using client.messengers.signin;
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
        private readonly ISignInTransfer signinTransfer;
        public PunchHoleReset(ISignInTransfer signinTransfer)
        {

            this.signinTransfer = signinTransfer;
        }
        public PunchHoleTypes Type => PunchHoleTypes.RESET;
        public async Task Execute(IConnection connection, PunchHoleRequestInfo info)
        {
            _ = signinTransfer.SignIn();
            await Task.CompletedTask;
        }
    }
}
