using client.messengers.punchHole;
using client.messengers.punchHole.tcp;
using common.server;
using common.server.model;
using System.Threading.Tasks;

namespace client.realize.messengers.punchHole.tcp.nutssb
{
    /// <summary>
    /// tcp打洞消息
    /// </summary>
    public sealed class PunchHoleTcpNutssB : IPunchHole
    {
        private readonly IPunchHoleTcp punchHoleTcp;
        public PunchHoleTcpNutssB(IPunchHoleTcp punchHoleTcp)
        {
            this.punchHoleTcp = punchHoleTcp;
        }

        public PunchHoleTypes Type => PunchHoleTypes.TCP_NUTSSB;

        public async Task Execute(IConnection connection, PunchHoleRequestInfo info)
        {
            await Task.CompletedTask;
            _ = punchHoleTcp.InputData(new PunchHoleStepModel
            {
                Connection = connection,
                RawData = info
            });
        }
    }
}
