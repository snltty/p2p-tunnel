using common.libs;
using common.server;
using common.server.model;
using server.messengers;
using server.messengers.singnin;
using server.service.validators;
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
        private readonly MessengerSender messengerSender;
        private readonly ISignInValidatorHandler signInValidatorHandler;

        public SignInMessenger(IClientSignInCaching clientSignInCache, MessengerSender messengerSender, ISignInValidatorHandler signInValidatorHandler)
        {
            this.clientSignInCache = clientSignInCache;
            this.messengerSender = messengerSender;
            this.signInValidatorHandler = signInValidatorHandler;
        }

        [MessengerId((ushort)SignInMessengerIds.SignIn)]
        public void SignIn(IConnection connection)
        {
            SignInParamsInfo model = new SignInParamsInfo();
            model.DeBytes(connection.ReceiveRequestWrap.Payload);

            uint access = (uint)EnumServiceAccess.None;
            //登入验证
            SignInResultInfo.SignInResultInfoCodes code = signInValidatorHandler.Validate(model, ref access);
            if (code != SignInResultInfo.SignInResultInfoCodes.OK)
            {
                connection.Write(new SignInResultInfo { Code = code }.ToBytes());
                return;
            }

            SignInCacheInfo client = clientSignInCache.Add(new SignInCacheInfo
            {
                Name = model.Name,
                GroupId = model.GroupId,
                LocalIps = model.LocalIps,
                ClientAccess = model.ClientAccess,
                ShortId = model.ShortId,
                LocalPort = model.LocalTcpPort,
                Connection = connection,
                UserAccess = access,
                Args = model.Args
            });
            signInValidatorHandler.Validated(client);

            connection.Write(new SignInResultInfo
            {
                ShortId = client.ShortId,
                ConnectionId = client.ConnectionId,
                Ip = connection.Address.Address,
                GroupId = client.GroupId,
                UserAccess = client.UserAccess
            }.ToBytes());
        }

        [MessengerId((ushort)SignInMessengerIds.Notify)]
        public void Notify(IConnection connection)
        {
            clientSignInCache.Notify(connection.ConnectId);
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
    }
}
