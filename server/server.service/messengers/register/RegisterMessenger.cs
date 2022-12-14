using common.libs;
using common.server;
using common.server.model;
using server.messengers.register;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace server.service.messengers.register
{

    /// <summary>
    /// 注册
    /// </summary>
    [MessengerIdRange((ushort)RegisterMessengerIds.Min, (ushort)RegisterMessengerIds.Max)]
    public sealed class RegisterMessenger : IMessenger
    {
        private readonly IClientRegisterCaching clientRegisterCache;
        private readonly IRegisterKeyValidator registerKeyValidator;
        private readonly MessengerSender messengerSender;
        private readonly IRelayValidator relayValidator;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientRegisterCache"></param>
        /// <param name="registerKeyValidator"></param>
        /// <param name="messengerSender"></param>
        /// <param name="relayValidator"></param>
        public RegisterMessenger(IClientRegisterCaching clientRegisterCache, IRegisterKeyValidator registerKeyValidator, MessengerSender messengerSender, IRelayValidator relayValidator)
        {
            this.clientRegisterCache = clientRegisterCache;
            this.registerKeyValidator = registerKeyValidator;
            this.messengerSender = messengerSender;
            this.relayValidator = relayValidator;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)RegisterMessengerIds.SignIn)]
        public async Task<byte[]> SignIn(IConnection connection)
        {
            RegisterParamsInfo model = new RegisterParamsInfo();
            model.DeBytes(connection.ReceiveRequestWrap.Payload);

            //验证key
            if (registerKeyValidator.Validate(model.GroupId) == false)
            {
                return new RegisterResultInfo { Code = RegisterResultInfo.RegisterResultInfoCodes.KEY_VERIFY }.ToBytes();
            }

            (RegisterResultInfo verify, RegisterCacheInfo client) = await VerifyAndAdd(model);
            if (verify != null)
            {
                return verify.ToBytes();
            }

            client.UpdateConnection(connection);
            client.AddTunnel(new TunnelRegisterCacheInfo
            {
                Port = connection.Address.Port,
                LocalPort = model.LocalUdpPort,
                Servertype = connection.ServerType,
                TunnelName = (ulong)(connection.ServerType == ServerType.TCP ? TunnelDefaults.TCP : TunnelDefaults.UDP),
                IsDefault = true,
            });
            return new RegisterResultInfo
            {
                ShortId = client.ShortId,
                Id = client.Id,
                Ip = connection.Address.Address,
                UdpPort = (ushort)(client.UdpConnection?.Address.Port ?? 0),
                TcpPort = (ushort)(client.TcpConnection?.Address.Port ?? 0),
                GroupId = client.GroupId,
                Relay = relayValidator.Validate(connection)
            }.ToBytes();

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)RegisterMessengerIds.Notify)]
        public void Notify(IConnection connection)
        {
            clientRegisterCache.Notify(connection);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)RegisterMessengerIds.SignOut)]
        public void SignOut(IConnection connection)
        {
            clientRegisterCache.Remove(connection.ConnectId);
            connection.Disponse();
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
                if (clientRegisterCache.Get(model.GroupId, model.Name, out client))
                {
                    await messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = client.OnLineConnection,
                        MessengerId = (ushort)RegisterMessengerIds.Offline
                    });
                    clientRegisterCache.Remove(client.Id);
                }
                client = new()
                {
                    Name = model.Name,
                    GroupId = model.GroupId,
                    LocalIps = model.LocalIps,
                    ClientAccess = model.ClientAccess,
                    Id = 0,
                    ShortId = model.ShortId,
                };
                clientRegisterCache.Add(client);
            }
            return (verify, client);
        }
    }
}
