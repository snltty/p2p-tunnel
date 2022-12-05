using client.messengers.register;
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
        private readonly RegisterStateInfo registerState;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="messengerSender"></param>
        /// <param name="registerState"></param>
        public CounterClientService(MessengerSender messengerSender, RegisterStateInfo registerState)
        {
            this.messengerSender = messengerSender;
            this.registerState = registerState;
        }

        /// <summary>
        /// 获取服务端信息
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public async Task<CounterResultInfo> Info(ClientServiceParamsInfo arg)
        {
            if (registerState.OnlineConnection != null)
            {
                var resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Payload = Helper.EmptyArray,
                    MessengerId = (ushort)CounterMessengerIds.Info,
                    Connection = registerState.OnlineConnection,
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
