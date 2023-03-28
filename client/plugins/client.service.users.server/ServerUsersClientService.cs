using client.messengers.singnin;
using client.service.ui.api.clientServer;
using common.libs.extends;
using common.server;
using common.server.model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace client.service.users.server
{
    /// <summary>
    /// 服务器账号管理
    /// </summary>
    public sealed class ServerUsersClientService : IClientService
    {
        private readonly MessengerSender messengerSender;
        private readonly SignInStateInfo signInStateInfo;

        public ServerUsersClientService(MessengerSender messengerSender, SignInStateInfo signInStateInfo)
        {
            this.messengerSender = messengerSender;
            this.signInStateInfo = signInStateInfo;
        }

        [ClientService(null)]
        public async Task<string> List(ClientServiceParamsInfo arg)
        {
            UserInfoPageModel page = arg.Content.DeJson<UserInfoPageModel>();
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInStateInfo.Connection,
                MessengerId = (ushort)UsersMessengerIds.Page,
                Payload = page.ToBytes()
            });
            if(resp.Code == MessageResponeCodes.OK)
            {
                return resp.Data.GetUTF8String();
            }
            return string.Empty;
        }


    }
}
