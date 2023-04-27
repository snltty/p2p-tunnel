using client.messengers.singnin;
using common.libs;
using common.server;
using common.server.model;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace client.realize.messengers.singnin
{
    public sealed class SignInMessengerSender
    {
        private readonly MessengerSender messengerSender;
        private readonly SignInStateInfo signInState;
        private readonly Config config;

        public SignInMessengerSender(MessengerSender messengerSender, SignInStateInfo signInState, Config config)
        {
            this.messengerSender = messengerSender;
            this.signInState = signInState;
            this.config = config;
        }
        public async Task<SignInResult> SignIn()
        {
            IPAddress[] localIps = new IPAddress[] { config.Client.LoopbackIp, signInState.LocalInfo.LocalIp };
            localIps = localIps.Concat(signInState.LocalInfo.RouteIps).ToArray();
            localIps = localIps.Concat(signInState.LocalInfo.Ipv6s).ToArray();

            SignInParamsInfo param = new SignInParamsInfo
            {
                ShortId = config.Client.ShortId,
                Id = 0,
                Name = config.Client.Name,
                Args = config.Client.Args,
                GroupId = config.Client.GroupId,
                LocalIps = localIps,
                LocalTcpPort = signInState.LocalInfo.Port,
                ClientAccess = (uint)config.Client.GetAccess()
            };
            param.Args.TryAdd("version",Helper.Version);

            MessageResponeInfo tcpResult = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInState.Connection,
                MessengerId = (ushort)SignInMessengerIds.SignIn,
                Payload = param.ToBytes(),
                Timeout = 15 * 1000,
            }).ConfigureAwait(false);

            if (tcpResult.Code != MessageResponeCodes.OK)
            {
                return new SignInResult { NetState = tcpResult };
            }

            SignInResultInfo tcpres = new SignInResultInfo();
            tcpres.DeBytes(tcpResult.Data);

            config.Client.ShortId = tcpres.ShortId;

            return new SignInResult { NetState = tcpResult, Data = tcpres };
        }
        public async Task<bool> Notify()
        {
            return await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = signInState.Connection,
                MessengerId = (ushort)SignInMessengerIds.Notify,
            }).ConfigureAwait(false);
        }

        public async Task Exit()
        {
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = signInState.Connection,
                MessengerId = (ushort)SignInMessengerIds.SignOut,
                Timeout = 2000
            }).ConfigureAwait(false);
        }

        public async Task Test()
        {
            await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInState.Connection,
                MessengerId = (ushort)SignInMessengerIds.Test,
                Timeout = 2000
            }).ConfigureAwait(false);
        }

    }

    public sealed class SignInResult
    {
        public MessageResponeInfo NetState { get; set; }
        public SignInResultInfo Data { get; set; }
    }
}
