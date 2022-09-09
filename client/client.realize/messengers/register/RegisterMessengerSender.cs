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
        private readonly ITcpServer tcpServer;
        private readonly IUdpServer udpServer;


        public RegisterMessengerSender(MessengerSender messengerSender, RegisterStateInfo registerState, Config config, ITcpServer tcpServer, IUdpServer udpServer)
        {
            this.messengerSender = messengerSender;
            this.registerState = registerState;
            this.config = config;
            this.tcpServer = tcpServer;
            this.udpServer = udpServer;
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
                    Memory = new RegisterParamsInfo
                    {
                        Id = id,
                        Name = param.ClientName,
                        GroupId = param.GroupId,
                        LocalIps = param.LocalIps,
                        Mac = param.Mac,
                        LocalTcpPort = param.LocalTcpPort,
                        LocalUdpPort = param.LocalUdpPort,
                        Key = param.Key
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
                    Memory = new RegisterParamsInfo
                    {
                        Id = id,
                        Name = param.ClientName,
                        GroupId = param.GroupId,
                        Mac = param.Mac,
                        LocalTcpPort = param.LocalTcpPort,
                        LocalUdpPort = param.LocalUdpPort,
                        Key = param.Key,
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
                Memory = Helper.EmptyArray,
                Path = "register/notify"
            }).ConfigureAwait(false);
        }
        public async Task Exit()
        {
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = registerState.OnlineConnection,
                Memory = Helper.EmptyArray,
                Path = "exit/execute",
                Timeout = 2000
            }).ConfigureAwait(false);
        }

        public async Task<int> GetTunnelPort(IConnection connection)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = connection,
                Memory = Helper.EmptyArray,
                Path = "register/port",
                Timeout = 2000
            }).ConfigureAwait(false);

            if (resp.Code == MessageResponeCodes.OK)
            {
                return resp.Data.Span.ToInt32();
            }
            return 0;
        }
        public async Task<ulong> AddTunnel(IConnection connection, ulong tunnelName, int port, int localPort)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = connection,
                Memory = new TunnelRegisterInfo { LocalPort = localPort, Port = port, TunnelName = tunnelName }.ToBytes(),
                Path = "register/addtunnel",
                Timeout = 2000
            }).ConfigureAwait(false);

            if (resp.Code == MessageResponeCodes.OK)
            {
                return resp.Data.Span.ToUInt64();
            }
            return 0;
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
        public string Key { get; set; } = string.Empty;

    }

    public class RegisterResult
    {
        public MessageResponeInfo NetState { get; set; }
        public RegisterResultInfo Data { get; set; }
    }
}
