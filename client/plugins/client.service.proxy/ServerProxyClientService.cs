using client.messengers.singnin;
using client.service.ui.api.clientServer;
using common.libs;
using common.libs.extends;
using common.proxy;
using common.server;
using common.server.model;
using System;
using System.Threading.Tasks;

namespace client.service.proxy
{
    /// <summary>
    /// proxy
    /// </summary>
    public sealed class ServerProxyClientService : IClientService
    {
        private readonly SignInStateInfo signInStateInfo;
        private readonly MessengerSender messengerSender;
        public ServerProxyClientService(SignInStateInfo signInStateInfo, MessengerSender messengerSender)
        {
            this.signInStateInfo = signInStateInfo;
            this.messengerSender = messengerSender;
        }

        public async Task<common.proxy.Config> Get(ClientServiceParamsInfo arg)
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInStateInfo.Connection,
                MessengerId = (ushort)ProxyMessengerIds.GetFirewall,
            });
            if (resp.Code == MessageResponeCodes.OK)
            {
                return resp.Data.GetUTF8String().DeJson<common.proxy.Config>();
            }
            return new common.proxy.Config();
        }

        public async Task<bool> Add(ClientServiceParamsInfo arg)
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInStateInfo.Connection,
                MessengerId = (ushort)ProxyMessengerIds.AddFirewall,
                Payload = arg.Content.ToUTF8Bytes()
            });
            if (resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray))
            {
                return true;
            }
            return false;
        }

        public async Task<bool> Remove(ClientServiceParamsInfo arg)
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInStateInfo.Connection,
                MessengerId = (ushort)ProxyMessengerIds.RemoveFirewall,
                Payload = uint.Parse(arg.Content).ToBytes(),
            });
            if (resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray))
            {
                return true;
            }
            return false;
        }
    }
}
