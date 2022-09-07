using client.messengers.punchHole;
using common.libs.extends;
using common.server;
using common.server.model;
using System;

namespace client.realize.messengers.punchHole
{
    public class PunchHoleMessenger : IMessenger
    {
        private readonly PunchHoleMessengerSender punchHoleMessengerSender;
        public PunchHoleMessenger(PunchHoleMessengerSender punchHoleMessengerSender)
        {

            this.punchHoleMessengerSender = punchHoleMessengerSender;
        }

        public void Execute(IConnection connection)
        {
            PunchHoleParamsInfo model = new PunchHoleParamsInfo();

            model.DeBytes(connection.ReceiveRequestWrap.Memory);

            punchHoleMessengerSender.OnPunchHole(new OnPunchHoleArg
            {
                Data = model,
                Connection = connection
            });
        }
    }
}
