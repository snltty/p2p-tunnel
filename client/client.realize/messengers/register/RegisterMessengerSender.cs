using client.messengers.register;
using common.libs;
using common.server;
using common.server.model;
using System;
using System.Net;
using System.Threading.Tasks;
using static common.server.model.RegisterResultInfo;

namespace client.realize.messengers.register
{
    public sealed class RegisterMessengerSender
    {
        private readonly MessengerSender messengerSender;
        private readonly RegisterStateInfo registerState;
        private readonly Config config;

        public RegisterMessengerSender(MessengerSender messengerSender, RegisterStateInfo registerState, Config config)
        {
            this.messengerSender = messengerSender;
            this.registerState = registerState;
            this.config = config;
        }
        public async Task<RegisterResult> Register(RegisterParams param)
        {
            MessageResponeInfo tcpResult = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = registerState.Connection,
                MessengerId = (ushort)RegisterMessengerIds.SignIn,
                Payload = new RegisterParamsInfo
                {
                    ShortId = param.ShortId,
                    Id = 0,
                    Name = param.ClientName,
                    GroupId = param.GroupId,
                    LocalIps = param.LocalIps,
                    LocalTcpPort = param.LocalTcpPort,
                    ClientAccess = (uint)param.ClientAccess
                }.ToBytes(),
                Timeout = param.Timeout,
            }).ConfigureAwait(false);

            if (tcpResult.Code != MessageResponeCodes.OK)
            {
                return new RegisterResult { NetState = tcpResult };
            }

            RegisterResultInfo tcpres = new RegisterResultInfo();
            tcpres.DeBytes(tcpResult.Data);
            RegisterResult regRes = new RegisterResult { NetState = tcpResult, Data = tcpres };
            param.ShortId = tcpres.ShortId;
            if (regRes != null)
            {
                return regRes;
            }

            return new RegisterResult
            {
                NetState = new MessageResponeInfo { Code = MessageResponeCodes.ERROR },
                Data = new RegisterResultInfo
                {
                    Code = RegisterResultInfoCodes.UNKNOW,
                }
            };
        }
        public async Task<bool> Notify()
        {
            return await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = registerState.Connection,
                MessengerId = (ushort)RegisterMessengerIds.Notify,
            }).ConfigureAwait(false);
        }

        public async Task Exit()
        {
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = registerState.Connection,
                MessengerId = (ushort)RegisterMessengerIds.SignOut,
                Timeout = 2000
            }).ConfigureAwait(false);
        }

        public async Task Test()
        {
            await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = registerState.Connection,
                MessengerId = (ushort)RegisterMessengerIds.Test,
                Timeout = 2000
            }).ConfigureAwait(false);
        }

    }

    public sealed class TunnelRegisterParams
    {
        public TunnelRegisterParams() { }
        public int TunnelName { get; set; } = -1;
        public int LocalPort { get; set; } = 0;
        public int Port { get; set; } = 0;
        public IConnection Connection { get; set; }
    }

    public sealed class RegisterParams
    {
        public byte ShortId { get; set; } = 0;
        public string GroupId { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public IPAddress[] LocalIps { get; set; } = Array.Empty<IPAddress>();
        public int Timeout { get; set; } = 15 * 1000;
        public ushort LocalTcpPort { get; set; } = 0;
        public EnumClientAccess ClientAccess { get; set; } = EnumClientAccess.None;

    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class RegisterResult
    {
        /// <summary>
        /// 
        /// </summary>
        public MessageResponeInfo NetState { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public RegisterResultInfo Data { get; set; }
    }
}
