using common.libs;
using common.server;
using common.server.model;
using server.messengers.singnin;
using System;
using System.Threading.Tasks;

namespace server.service.messengers.singnin
{

    /// <summary>
    /// 注册
    /// </summary>
    [MessengerIdRange((ushort)SignInMessengerIds.Min, (ushort)SignInMessengerIds.Max)]
    public sealed class SignInMessenger : IMessenger
    {
        private readonly IClientSignInCaching clientSignInCache;
        private readonly ISignInValidator registerKeyValidator;
        private readonly MessengerSender messengerSender;
        private readonly IRelayValidator relayValidator;

        public SignInMessenger(IClientSignInCaching clientSignInCache, ISignInValidator registerKeyValidator, MessengerSender messengerSender, IRelayValidator relayValidator)
        {
            this.clientSignInCache = clientSignInCache;
            this.registerKeyValidator = registerKeyValidator;
            this.messengerSender = messengerSender;
            this.relayValidator = relayValidator;
        }

        [MessengerId((ushort)SignInMessengerIds.SignIn)]
        public byte[] SignIn(IConnection connection)
        {
            SignInParamsInfo model = new SignInParamsInfo();
            model.DeBytes(connection.ReceiveRequestWrap.Payload);

            //验证key
            if (registerKeyValidator.Validate(model.GroupId) == false)
            {
                return new SignInResultInfo { Code = SignInResultInfo.SignInResultInfoCodes.KEY_VERIFY }.ToBytes();
            }

            (SignInResultInfo verify, SignInCacheInfo client) = VerifyAndAdd(model, connection);
            if (verify != null)
            {
                return verify.ToBytes();
            }

            client.UpdateConnection(connection);
            return new SignInResultInfo
            {
                ShortId = client.ShortId,
                Id = client.Id,
                Ip = connection.Address.Address,
                GroupId = client.GroupId,
                Relay = relayValidator.Validate(connection)
            }.ToBytes();

        }

        [MessengerId((ushort)SignInMessengerIds.Notify)]
        public void Notify(IConnection connection)
        {
            clientSignInCache.Notify(connection);
        }

        [MessengerId((ushort)SignInMessengerIds.SignOut)]
        public void SignOut(IConnection connection)
        {
            clientSignInCache.Remove(connection.ConnectId);
            connection.Disponse();
        }

        [MessengerId((ushort)SignInMessengerIds.Test)]
        public async Task Test(IConnection connection)
        {
            var res = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = connection,
                MessengerId = (ushort)HeartMessengerIds.Alive,
                Timeout = 2000,

            });
            Console.WriteLine(res.Code);
        }

        private (SignInResultInfo, SignInCacheInfo) VerifyAndAdd(SignInParamsInfo model, IConnection connection)
        {
            SignInResultInfo verify = null;
            ///第一次注册，检查有没有重名
            if (clientSignInCache.Get(model.GroupId, model.Name, out SignInCacheInfo client))
            {
                clientSignInCache.Remove(client.Id);
                verify = new SignInResultInfo { Code = SignInResultInfo.SignInResultInfoCodes.SAME_NAMES };
            }
            else
            {
                client = new()
                {
                    Name = model.Name,
                    GroupId = model.GroupId,
                    LocalIps = model.LocalIps,
                    ClientAccess = model.ClientAccess,
                    Id = 0,
                    ShortId = model.ShortId,
                    LocalPort = model.LocalTcpPort,
                    Port = connection.Address.Port
                };
                clientSignInCache.Add(client);
            }
            return (verify, client);
        }
    }
}
