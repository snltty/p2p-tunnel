using client.messengers.punchHole;
using common.server;
using common.server.model;
using System.Threading.Tasks;

namespace client.realize.messengers.punchHole
{
    [MessengerIdRange((ushort)PunchHoleMessengerIds.Min, (ushort)PunchHoleMessengerIds.Max)]
    public sealed class PunchHoleMessenger : IMessenger
    {
        private readonly PunchHoleMessengerSender punchHoleMessengerSender;
        public PunchHoleMessenger(PunchHoleMessengerSender punchHoleMessengerSender)
        {

            this.punchHoleMessengerSender = punchHoleMessengerSender;
        }

        [MessengerId((ushort)PunchHoleMessengerIds.Response)]
        public void Response(IConnection connection)
        {
            PunchHoleResponseInfo model = new PunchHoleResponseInfo();

            model.DeBytes(connection.ReceiveRequestWrap.Payload);

            punchHoleMessengerSender.OnResponse(model);
        }

        [MessengerId((ushort)PunchHoleMessengerIds.Request)]
        public async Task Request(IConnection connection)
        {
            PunchHoleRequestInfo model = new PunchHoleRequestInfo();
            model.DeBytes(connection.ReceiveRequestWrap.Payload);

            await punchHoleMessengerSender.Response(connection, model);

            punchHoleMessengerSender.OnPunchHole(new OnPunchHoleArg
            {
                Data = model,
                Connection = connection
            });
        }
    }
}
