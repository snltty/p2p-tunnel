using common.libs;
using common.server;
using common.server.model;
using server.messengers;
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
        private readonly ISignInValidator signInValidator;
        private readonly MessengerSender messengerSender;
        private readonly IUserStore userStore;

        public SignInMessenger(IClientSignInCaching clientSignInCache, ISignInValidator signInValidator, MessengerSender messengerSender, IUserStore userStore)
        {
            this.clientSignInCache = clientSignInCache;
            this.signInValidator = signInValidator;
            this.messengerSender = messengerSender;
            this.userStore = userStore;
        }

        [MessengerId((ushort)SignInMessengerIds.SignIn)]
        public byte[] SignIn(IConnection connection)
        {
            SignInParamsInfo model = new SignInParamsInfo();
            model.DeBytes(connection.ReceiveRequestWrap.Payload);

            //获取用户
            userStore.Get(model.Account, model.Password, out UserInfo user);
            //验证登入权限
            SignInResultInfo.SignInResultInfoCodes code = signInValidator.Validate(ref user);
            if (code != SignInResultInfo.SignInResultInfoCodes.OK)
            {
                return new SignInResultInfo { Code = code }.ToBytes();
            }

            (SignInResultInfo verify, SignInCacheInfo client) = VerifyAndAdd(model, user, connection);
            if (verify != null)
            {
                return verify.ToBytes();
            }

            client.UpdateConnection(connection);
            return new SignInResultInfo
            {
                ShortId = client.ShortId,
                Id = client.ConnectionId,
                Ip = connection.Address.Address,
                GroupId = client.GroupId,
                Access = user.Access,
                EndTime = user.EndTime,
                NetFlow = user.NetFlow
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

        private (SignInResultInfo, SignInCacheInfo) VerifyAndAdd(SignInParamsInfo model, UserInfo user, IConnection connection)
        {
            SignInResultInfo verify = null;
            ///第一次注册，检查有没有重名
            if (clientSignInCache.Get(model.GroupId, model.Name, out SignInCacheInfo client))
            {
                clientSignInCache.Remove(client.ConnectionId);
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
                    ConnectionId = 0,
                    UserId = user.ID,
                    ShortId = model.ShortId,
                    LocalPort = model.LocalTcpPort,
                    Port = connection.Address.Port,
                    NetFlow = user.NetFlow,
                    UserAccess = user.Access
                };
                clientSignInCache.Add(client);
            }
            return (verify, client);
        }
    }
}
