using client.messengers.punchHole;
using common.server;
using common.server.model;
using System.Threading.Tasks;

namespace client.realize.messengers.punchHole
{
    /// <summary>
    /// 打洞消息
    /// </summary>
    [MessengerIdRange((ushort)PunchHoleMessengerIds.Min, (ushort)PunchHoleMessengerIds.Max)]
    public sealed class PunchHoleMessenger : IMessenger
    {
        private readonly PunchHoleMessengerSender punchHoleMessengerSender;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="punchHoleMessengerSender"></param>
        public PunchHoleMessenger(PunchHoleMessengerSender punchHoleMessengerSender)
        {

            this.punchHoleMessengerSender = punchHoleMessengerSender;
        }

        /// <summary>
        /// 打洞消息回执
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)PunchHoleMessengerIds.Response)]
        public void Response(IConnection connection)
        {
            PunchHoleResponseInfo model = new PunchHoleResponseInfo();
            model.DeBytes(connection.ReceiveRequestWrap.Payload);

            punchHoleMessengerSender.OnResponse(model);
        }

        /// <summary>
        /// 打洞消息
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)PunchHoleMessengerIds.Request)]
        public async Task Request(IConnection connection)
        {
            PunchHoleRequestInfo model = new PunchHoleRequestInfo();
            model.DeBytes(connection.ReceiveRequestWrap.Payload);
            await punchHoleMessengerSender.OnPunchHole(new OnPunchHoleArg
            {
                Data = model,
                Connection = connection
            });
        }
    }
}
