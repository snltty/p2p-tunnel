using client.messengers.singnin;
using client.service.ui.api.clientServer;
using common.server;
using System.Threading.Tasks;

namespace client.service.vea.server
{
    public sealed class ServerVeaClientService : IClientService
    {
        private readonly MessengerSender messengerSender;
        private readonly SignInStateInfo signInStateInfo;

        public ServerVeaClientService(MessengerSender messengerSender, SignInStateInfo signInStateInfo)
        {
            this.messengerSender = messengerSender;
            this.signInStateInfo = signInStateInfo;
        }

        public async Task<string> List(ClientServiceParamsInfo arg)
        {
            return string.Empty;
        }
    }
}
