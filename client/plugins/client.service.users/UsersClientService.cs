using client.messengers.singnin;
using client.service.ui.api.clientServer;
using common.libs.extends;
using common.server;
using common.server.model;
using common.user;
using System.Threading.Tasks;

namespace client.service.users.server
{
    /// <summary>
    /// 服务器账号管理
    /// </summary>
    public sealed class UsersClientService : IClientService
    {
        private readonly MessengerSender messengerSender;
        private readonly SignInStateInfo signInStateInfo;
        private readonly IUserMapInfoCaching userMapInfoCaching;

        public UsersClientService(MessengerSender messengerSender, SignInStateInfo signInStateInfo, IUserMapInfoCaching userMapInfoCaching)
        {
            this.messengerSender = messengerSender;
            this.signInStateInfo = signInStateInfo;
            this.userMapInfoCaching = userMapInfoCaching;
        }

        public async Task<string> List(ClientServiceParamsInfo arg)
        {
            UserInfoPageModel page = arg.Content.DeJson<UserInfoPageModel>();
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInStateInfo.Connection,
                MessengerId = (ushort)UsersMessengerIds.Page,
                Payload = page.ToBytes()
            });
            if (resp.Code == MessageResponeCodes.OK)
            {
                UserInfoPageResultModel pageResult = resp.Data.GetUTF8String().DeJson<UserInfoPageResultModel>();
                foreach (UserInfo item in pageResult.Data)
                {
                    if (userMapInfoCaching.Get(item.ID, out UserMapInfo map))
                    {
                        item.Access = map.Access;
                    }
                    else
                    {
                        item.Access = (uint)EnumServiceAccess.None;
                    }
                }
                return pageResult.ToJson();
            }

            return string.Empty;
        }

        public async Task<string> Update(ClientServiceParamsInfo arg)
        {
            UserMapInfo map = arg.Content.DeJson<UserMapInfo>();
            if (userMapInfoCaching.Get(map.ID, out UserMapInfo mapInfo))
            {
                mapInfo.Access = map.Access;
            }
            else
            {
                await userMapInfoCaching.Add(map);
            }
            return string.Empty;
        }
    }

}
