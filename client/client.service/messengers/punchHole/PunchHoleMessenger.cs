using client.messengers.punchHole;
using common.libs.extends;
using common.server;
using common.server.model;

namespace client.service.messengers.punchHole
{
    public class PunchHoleMessenger : IMessenger
    {
        private readonly PunchHoleMessengerSender   punchHoleMessengerSender;
        public PunchHoleMessenger(PunchHoleMessengerSender punchHoleMessengerSender)
        {

            this.punchHoleMessengerSender = punchHoleMessengerSender;
        }

        public void Execute(IConnection connection)
        {
            PunchHoleParamsInfo model = connection.ReceiveRequestWrap.Memory.DeBytes<PunchHoleParamsInfo>();

            punchHoleMessengerSender.OnPunchHole(new OnPunchHoleArg
            {
                Data = model,
                Connection = connection
            });
        }
    }
}
