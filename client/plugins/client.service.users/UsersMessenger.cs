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
                    _ = messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = client.Connection,
                        MessengerId = (ushort)UsersMessengerIds.SignIn,
                        Payload = new UserSignInfo { ConnectionId = signInStateInfo.ConnectId, UserId = user.ID }.ToBytes()
                    });
                }
            });
        }


        [MessengerId((ushort)UsersMessengerIds.SignIn)]
        public void SignIn(IConnection connection)
        {
            UserSignInfo userSignInfo = new UserSignInfo();
            userSignInfo.DeBytes(connection.ReceiveRequestWrap.Payload);
            userSignInfo.ConnectionId = connection.FromConnection.ConnectId;

            //去服务器验证登录是否正确，
            _ = messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInStateInfo.Connection,
                MessengerId = (ushort)UsersMessengerIds.SignIn,
                Payload = userSignInfo.ToBytes()
            }).ContinueWith(async (resp) =>
            {
                if (resp.Result.Code == MessageResponeCodes.OK && resp.Result.Data.Span.SequenceEqual(Helper.TrueArray))
                {
                    if (clientInfoCaching.Get(connection.FromConnection.ConnectId, out ClientInfo client))
                    {
                        if (userMapInfoCaching.Get(userSignInfo.UserId, out UserMapInfo map) == false)
                        {
                            map = new UserMapInfo { Access = 0, ID = userSignInfo.UserId, ConnectionId = userSignInfo.ConnectionId };
                            await userMapInfoCaching.Add(map);
                        }
                        //更新客户端的权限值
                        client.UserAccess = map.Access;
                        map.ConnectionId = userSignInfo.ConnectionId;
                    }
                }
            });
        }
    }
}
