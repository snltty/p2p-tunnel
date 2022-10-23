using client.messengers.register;
using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using System;
using System.Net;
using System.Threading.Tasks;
using static common.server.model.RegisterResultInfo;

namespace client.realize.messengers.register
{
    public class RegisterMessengerSender
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
            ulong id = 0;
            RegisterResult regRes = null;
            if (config.Client.UseUdp)
            {
                MessageResponeInfo result = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = registerState.UdpConnection,
                    Path = "register/Execute",
                    Payload = new RegisterParamsInfo
                    {
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
            }
            if (config.Client.UseTcp)
            {
                MessageResponeInfo tcpResult = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = registerState.TcpConnection,
                    Path = "register/Execute",
                    Payload = new RegisterParamsInfo
                    {
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
        public async Task<bool> Notify()
        {
            return await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = registerState.OnlineConnection,
                Payload = Helper.EmptyArray,
                Path = "register/notify"
            }).ConfigureAwait(false);
        }
        public async Task Exit()
        {
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = registerState.OnlineConnection,
                Payload = Helper.EmptyArray,
                Path = "exit/execute",
                Timeout = 2000
            }).ConfigureAwait(false);
        }


    }

    public class TunnelRegisterParams
    {
        public TunnelRegisterParams() { }

        public int TunnelName { get; set; } = -1;
        public int LocalPort { get; set; } = 0;
        public int Port { get; set; } = 0;
        public IConnection Connection { get; set; }
    }

    public class RegisterParams
    {
        public string GroupId { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public IPAddress[] LocalIps { get; set; } = Array.Empty<IPAddress>();
        public string Mac { get; set; } = string.Empty;
        public int Timeout { get; set; } = 15 * 1000;
        public int LocalUdpPort { get; set; } = 0;
        public int LocalTcpPort { get; set; } = 0;
        public EnumClientAccess ClientAccess { get; set; } = EnumClientAccess.None;

    }

    public class RegisterResult
    {
        public MessageResponeInfo NetState { get; set; }
        public RegisterResultInfo Data { get; set; }
    }
}
