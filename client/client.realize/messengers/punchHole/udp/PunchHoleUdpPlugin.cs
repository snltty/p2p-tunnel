using client.messengers.punchHole;
using client.messengers.punchHole.udp;
using common.libs;
using common.server;
using common.server.model;
using System.Threading.Tasks;

namespace client.realize.messengers.punchHole.udp
{
    /// <summary>
    /// udp打洞消息
    /// </summary>
    public sealed class PunchHoleUdp : IPunchHole
    {
        private readonly IPunchHoleUdp punchHoleUdp;
        public PunchHoleUdp(IPunchHoleUdp punchHoleUdp)
        {
            this.punchHoleUdp = punchHoleUdp;
        }

        public PunchHoleTypes Type => PunchHoleTypes.UDP;

        public async Task Execute(IConnection connection, PunchHoleRequestInfo info)
        {
            await punchHoleUdp.InputData(new PunchHoleStepModel { Connection = connection, RawData = info });
        }
    }

}
