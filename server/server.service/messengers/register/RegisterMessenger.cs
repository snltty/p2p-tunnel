using common.libs;
using common.server;
using common.server.model;
using server.messengers.register;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace server.service.messengers.register
{

    [MessengerIdRange((ushort)RegisterMessengerIds.Min, (ushort)RegisterMessengerIds.Max)]
    public class RegisterMessenger : IMessenger
    {
        private readonly IClientRegisterCaching clientRegisterCache;
        private readonly IRegisterKeyValidator registerKeyValidator;
        private readonly MessengerSender messengerSender;
        private readonly IRelayValidator relayValidator;

        public RegisterMessenger(IClientRegisterCaching clientRegisterCache, IRegisterKeyValidator registerKeyValidator, MessengerSender messengerSender, IRelayValidator relayValidator)
        {
            this.clientRegisterCache = clientRegisterCache;
            this.registerKeyValidator = registerKeyValidator;
            this.messengerSender = messengerSender;
            this.relayValidator = relayValidator;
        }

        [MessengerId((ushort)RegisterMessengerIds.SignIn)]
        public async Task<byte[]> Execute(IConnection connection)
        {
            RegisterParamsInfo model = new RegisterParamsInfo();
            model.DeBytes(connection.ReceiveRequestWrap.Payload);
            //验证key
            if (registerKeyValidator.Validate(model.GroupId) == false)
            {
                return new RegisterResultInfo { Code = RegisterResultInfo.RegisterResultInfoCodes.KEY_VERIFY }.ToBytes();
            }

            return connection.ServerType switch
            {
                ServerType.UDP => await Udp(connection, model),
                ServerType.TCP => await Tcp(connection, model),
                _ => new RegisterResultInfo { Code = RegisterResultInfo.RegisterResultInfoCodes.UNKNOW }.ToBytes()
            };
        }

        private async Task<byte[]> Udp(IConnection connection, RegisterParamsInfo model)
        {
            (RegisterResultInfo verify, RegisterCacheInfo client) = await VerifyAndAdd(model);
            if (verify != null)
            {
                return verify.ToBytes();
            }

            client.UpdateUdpInfo(connection);
            client.AddTunnel(new TunnelRegisterCacheInfo
            {
                Port = connection.Address.Port,
                LocalPort = model.LocalUdpPort,
                Servertype = ServerType.UDP,
                TunnelName = (ulong)TunnelDefaults.UDP,
                IsDefault = true,
            });

            return new RegisterResultInfo
            {
                Id = client.Id,
                Ip = connection.Address.Address,
                UdpPort = connection.Address.Port,
                TcpPort = client.TcpConnection?.Address.Port ?? 0,
                GroupId = client.GroupId,
                Relay = relayValidator.Validate(client.GroupId)
            }.ToBytes();
        }
        private async Task<byte[]> Tcp(IConnection connection, RegisterParamsInfo model)
        {
            (RegisterResultInfo verify, RegisterCacheInfo client) = await VerifyAndAdd(model);
            if (verify != null)
            {
                return verify.ToBytes();
            }

            client.UpdateTcpInfo(connection);
            client.AddTunnel(new TunnelRegisterCacheInfo
            {
                Port = connection.Address.Port,
                LocalPort = model.LocalTcpPort,
                Servertype = ServerType.TCP,
                TunnelName = (ulong)TunnelDefaults.TCP,
                IsDefault = true,
            });

            return new RegisterResultInfo
            {
                Id = client.Id,
                Ip = connection.Address.Address,
                UdpPort = client.UdpConnection?.Address.Port ?? 0,
                TcpPort = connection.Address.Port,
                GroupId = client.GroupId,
                Relay = relayValidator.Validate(client.GroupId),
            }.ToBytes();
        }
        private async Task<(RegisterResultInfo, RegisterCacheInfo)> VerifyAndAdd(RegisterParamsInfo model)
        {
            RegisterResultInfo verify = null;
            RegisterCacheInfo client;
            //不是第一次注册
            if (model.Id > 0)
            {
                if (clientRegisterCache.Get(model.Id, out client) == false)
                {
                    verify = new RegisterResultInfo { Code = RegisterResultInfo.RegisterResultInfoCodes.VERIFY };
                }
            }
            else
            {
                //第一次注册，检查有没有重名
                client = clientRegisterCache.GetBySameGroup(model.GroupId, model.Name).FirstOrDefault();
                if (client != null)
                {
                    bool alive = await GetAlive(client.OnLineConnection);
                    if (!alive)
                    {
                        clientRegisterCache.Remove(client.Id);
                        client = null;
                    }
                }

                if (client == null)
                {
                    client = new()
                    {
                        Name = model.Name,
                        GroupId = model.GroupId,
                        LocalIps = model.LocalIps,
                        ClientAccess = model.ClientAccess,
                        Id = 0
                    };
                    clientRegisterCache.Add(client);
                }
                else
                {
                    verify = new RegisterResultInfo { Code = RegisterResultInfo.RegisterResultInfoCodes.SAME_NAMES };
                }
            }
            return (verify, client);
        }

        [MessengerId((ushort)RegisterMessengerIds.Notify)]
        public void Notify(IConnection connection)
        {
            clientRegisterCache.Notify(connection);
        }

        private async Task<bool> GetAlive(IConnection connection)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = connection,
                Payload = Helper.EmptyArray,
                MessengerId =(ushort)HeartMessengerIds.Alive,
                Timeout = 2000,
            });
            return resp.Code == MessageResponeCodes.OK && Helper.TrueArray.AsSpan().SequenceEqual(resp.Data.Span);
        }


        [MessengerId((ushort)RegisterMessengerIds.SignOut)]
        public void Exit(IConnection connection)
        {
            connection.Disponse();
        }
    }
}
