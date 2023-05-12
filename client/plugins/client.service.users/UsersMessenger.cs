using client.messengers.clients;
using client.messengers.singnin;
using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using common.user;
using System;
using System.Threading.Tasks;

namespace client.service.users
{
    [MessengerIdRange((ushort)UsersMessengerIds.Min, (ushort)UsersMessengerIds.Max)]
    public sealed class UsersMessenger : IMessenger
    {
        private readonly IUserMapInfoCaching userMapInfoCaching;
        private readonly MessengerSender messengerSender;
        private readonly SignInStateInfo signInStateInfo;
        private readonly IClientInfoCaching clientInfoCaching;

        public UsersMessenger(IUserMapInfoCaching userMapInfoCaching, MessengerSender messengerSender, SignInStateInfo signInStateInfo, IClientInfoCaching clientInfoCaching)
        {
            this.userMapInfoCaching = userMapInfoCaching;
            this.messengerSender = messengerSender;
            this.signInStateInfo = signInStateInfo;
            this.clientInfoCaching = clientInfoCaching;
            clientInfoCaching.OnOnline += OnOnline;
        }

        /// <summary>
        /// 向目标客户端登入，在目标客户端进行权限缓存，下次去和目标端通信时，才可能有权限
        /// </summary>
        /// <param name="client"></param>
        private void OnOnline(ClientInfo client)
        {
            messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInStateInfo.Connection,
                MessengerId = (ushort)UsersMessengerIds.Info
            }).ContinueWith((resp) =>
            {
                if (resp.Result.Code == MessageResponeCodes.OK)
                {
                    UserInfo user = resp.Result.Data.GetUTF8String().DeJson<UserInfo>();
                    messengerSender.SendReply(new MessageRequestWrap
                    {
                        Connection = client.Connection,
                        MessengerId = (ushort)UsersMessengerIds.SignIn,
                        Payload = new UserSignInfo { ConnectionId = signInStateInfo.ConnectId, ID = user.ID }.ToBytes()
                    }).ContinueWith((resp) =>
                    {
                        if (resp.Result.Code == MessageResponeCodes.OK && resp.Result.Data.Span.SequenceEqual(Helper.TrueArray))
                        {
                            Logger.Instance.Debug($"向 {client.Name} 节点请求账号权限成功");
                        }
                        else
                        {
                            Logger.Instance.Debug($"向 {client.Name} 节点请求账号权限失败");
                        }
                    });
                }
            });
        }


        [MessengerId((ushort)UsersMessengerIds.SignIn)]
        public async Task SignIn(IConnection connection)
        {
            UserSignInfo userSignInfo = new UserSignInfo();
            userSignInfo.DeBytes(connection.ReceiveRequestWrap.Payload);
            userSignInfo.ConnectionId = connection.FromConnection.ConnectId;

            //去服务器验证登录是否正确，
            MessageResponeInfo resp = await messengerSender.SendReply(new common.server.model.MessageRequestWrap
            {
                Connection = signInStateInfo.Connection,
                MessengerId = (ushort)UsersMessengerIds.SignIn,
                Payload = userSignInfo.ToBytes()
            });
            if (resp.Code == common.server.model.MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray))
            {
                if (userMapInfoCaching.Get(userSignInfo.ID, out UserMapInfo map) && clientInfoCaching.Get(connection.FromConnection.ConnectId, out ClientInfo client))
                {
                    //更新客户端的权限值
                    client.UserAccess = map.Access;
                    connection.FromConnection.Write(Helper.TrueArray);
                    return;
                }
            }

            connection.FromConnection.Write(Helper.FalseArray);
        }
    }
}
