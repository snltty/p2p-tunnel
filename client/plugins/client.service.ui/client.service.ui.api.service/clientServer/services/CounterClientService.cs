using client.messengers.singnin;
using client.service.ui.api.clientServer;
using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using System.Threading.Tasks;

namespace client.service.ui.api.service.clientServer.services
{
    /// <summary>
    /// 服务端信息
    /// </summary>
    public sealed class CounterClientService : IClientService
    {
        private readonly MessengerSender messengerSender;
        private readonly SignInStateInfo signInState;

        public CounterClientService(MessengerSender messengerSender, SignInStateInfo signInState)
        {
            this.messengerSender = messengerSender;
            this.signInState = signInState;
        }

        public async Task<CounterResultInfo> Info(ClientServiceParamsInfo arg)
        {
            if (signInState.Connection != null)
            {
                var resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    MessengerId = (ushort)CounterMessengerIds.Info,
                    Connection = signInState.Connection,
                    Timeout = 15000
                }).ConfigureAwait(false);
                if (resp.Code == MessageResponeCodes.OK)
                {
                    CounterResultInfo res = new CounterResultInfo();
                    res.DeBytes(resp.Data);
                    return res;
                }
                else
                {
                    arg.SetCode(ClientServiceResponseCodes.Error, resp.Code.GetDesc((byte)resp.Code));
                }
            }
            else
            {
                arg.SetCode(ClientServiceResponseCodes.Error, "未注册");
            }
            return null;
        }
    }
}
