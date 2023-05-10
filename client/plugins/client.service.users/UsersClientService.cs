using client.messengers.clients;
using client.service.ui.api.clientServer;
using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using common.user;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace client.service.users.server
{
    /// <summary>
    /// 服务器账号管理
    /// </summary>
    public sealed class UsersClientService : IClientService
    {
        private readonly MessengerSender messengerSender;
        private readonly IClientInfoCaching clientInfoCaching;
        private readonly IUserStore userStore;

        public UsersClientService(MessengerSender messengerSender, IClientInfoCaching clientInfoCaching, IUserStore userStore)
        {
            this.messengerSender = messengerSender;
            this.clientInfoCaching = clientInfoCaching;
            this.userStore = userStore;
        }

        public string List(ClientServiceParamsInfo arg)
        {
            UserInfoPageModel page = arg.Content.DeJson<UserInfoPageModel>();

            return new UserInfoPageResultModel
            {
                Count = userStore.Count(),
                Page = page.Page,
                PageSize = page.PageSize,
                Data = userStore.Get(page.Page, page.PageSize, page.Sort, page.Account).ToList()
            }.ToJson();
        }

        public string Add(ClientServiceParamsInfo arg)
        {
            bool res = userStore.Add(arg.Content.DeJson<UserInfo>());
            return string.Empty;
        }
        public string Password(ClientServiceParamsInfo arg)
        {
            PasswordModel passwordModel = arg.Content.DeJson<PasswordModel>();
            bool res = userStore.UpdatePassword(passwordModel.ID, passwordModel.Password);
            return string.Empty;
        }
        public string Remove(ClientServiceParamsInfo arg)
        {
            bool res = userStore.Remove(ulong.Parse(arg.Content));
            return string.Empty;
        }

        public async Task<bool> SignIn(ClientServiceParamsInfo arg)
        {
            ulong toid = ulong.Parse(arg.Content);
            if (clientInfoCaching.Get(toid, out ClientInfo client))
            {
                var resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = client.Connection,
                    MessengerId = (ushort)UsersMessengerIds.SignIn,
                    Payload = arg.Content.ToUTF8Bytes(),
                });
                if (resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray))
                {
                    return true;
                }
            }
            return false;
        }

        public async Task<string> Info(ClientServiceParamsInfo arg)
        {
            ulong toid = ulong.Parse(arg.Content);
            if (clientInfoCaching.Get(toid, out ClientInfo client))
            {
                var resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = client.Connection,
                    MessengerId = (ushort)UsersMessengerIds.Info
                });
                if (resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.FalseArray) == false)
                {
                    return resp.Data.GetUTF8String();
                }
            }
            return new UserInfo().ToJson();
        }
    }

    public class PasswordModel
    {
        public ulong ID { get; set; }
        public string Password { get; set; }
    }
}
