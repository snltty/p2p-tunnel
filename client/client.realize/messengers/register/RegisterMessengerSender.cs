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
    /// <summary>
    /// 
    /// </summary>
    public sealed class RegisterMessengerSender
    {
        private readonly MessengerSender messengerSender;
        private readonly RegisterStateInfo registerState;
        private readonly Config config;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="messengerSender"></param>
        /// <param name="registerState"></param>
        /// <param name="config"></param>
        public RegisterMessengerSender(MessengerSender messengerSender, RegisterStateInfo registerState, Config config)
        {
            this.messengerSender = messengerSender;
            this.registerState = registerState;
            this.config = config;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<RegisterResult> Register(RegisterParams param)
        {
            ulong id = 0;
            RegisterResult regRes = null;
            if (config.Client.UseUdp)
            {
                MessageResponeInfo result = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = registerState.UdpConnection,
                    MessengerId = (ushort)RegisterMessengerIds.SignIn,
                    Payload = new RegisterParamsInfo
                    {
                        ShortId = param.ShortId,
                        Id = id,
                        Name = param.ClientName,
                        GroupId = param.GroupId,
                        LocalIps = param.LocalIps,
                        LocalTcpPort = param.LocalTcpPort,
                        LocalUdpPort = param.LocalUdpPort,
                        ClientAccess = (uint)param.ClientAccess
                    }.ToBytes(),
                    Timeout = param.Timeout,
                }).ConfigureAwait(false);
                if (result.Code != MessageResponeCodes.OK)
                {
                    return new RegisterResult { NetState = result };
                }
                RegisterResultInfo res = new RegisterResultInfo();
                res.DeBytes(result.Data);
                regRes = new RegisterResult { NetState = result, Data = res };
                if (res.Code != RegisterResultInfoCodes.OK)
                {
                    return regRes;
                }
                id = res.Id;
                param.ShortId = res.ShortId;
            }
            if (config.Client.UseTcp)
            {
                MessageResponeInfo tcpResult = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = registerState.TcpConnection,
                    MessengerId = (ushort)RegisterMessengerIds.SignIn,
                    Payload = new RegisterParamsInfo
                    {
                        ShortId = param.ShortId,
                        Id = id,
                        Name = param.ClientName,
                        GroupId = param.GroupId,
                        LocalIps = param.LocalIps,
                        LocalTcpPort = param.LocalTcpPort,
                        LocalUdpPort = param.LocalUdpPort,
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
                regRes = new RegisterResult { NetState = tcpResult, Data = tcpres };
                param.ShortId = tcpres.ShortId;
            }
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
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<bool> Notify()
        {
            return await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = registerState.OnlineConnection,
                MessengerId = (ushort)RegisterMessengerIds.Notify,
            }).ConfigureAwait(false);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task Exit()
        {
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = registerState.OnlineConnection,
                MessengerId = (ushort)RegisterMessengerIds.SignOut,
                Timeout = 2000
            }).ConfigureAwait(false);
        }

        public async Task Test()
        {
            await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = registerState.OnlineConnection,
                MessengerId = (ushort)RegisterMessengerIds.Test,
                Timeout = 2000
            }).ConfigureAwait(false);
        }

    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class TunnelRegisterParams
    {
        /// <summary>
        /// 
        /// </summary>
        public TunnelRegisterParams() { }
        /// <summary>
        /// 
        /// </summary>
        public int TunnelName { get; set; } = -1;
        /// <summary>
        /// 
        /// </summary>
        public int LocalPort { get; set; } = 0;
        /// <summary>
        /// 
        /// </summary>
        public int Port { get; set; } = 0;
        /// <summary>
        /// 
        /// </summary>
        public IConnection Connection { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class RegisterParams
    {
        /// <summary>
        /// 
        /// </summary>
        public byte ShortId { get; set; } = 0;
        /// <summary>
        /// 
        /// </summary>
        public string GroupId { get; set; } = string.Empty;
        /// <summary>
        /// 
        /// </summary>
        public string ClientName { get; set; } = string.Empty;
        /// <summary>
        /// 
        /// </summary>
        public IPAddress[] LocalIps { get; set; } = Array.Empty<IPAddress>();
        /// <summary>
        /// 
        /// </summary>
        public int Timeout { get; set; } = 15 * 1000;
        /// <summary>
        /// 
        /// </summary>
        public ushort LocalUdpPort { get; set; } = 0;
        /// <summary>
        /// 
        /// </summary>
        public ushort LocalTcpPort { get; set; } = 0;
        /// <summary>
        /// 
        /// </summary>
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
