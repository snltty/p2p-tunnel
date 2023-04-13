using common.libs;
using common.server;
using common.server.model;
using server.messengers;
using server.messengers.singnin;
using server.service.users.model;
using System;
using System.Collections.Generic;

namespace server.service.users
{
    /// <summary>
    /// 登入权限验证
    /// </summary>
    public sealed class SignInAccessValidator : ISignInValidator
    {
        private readonly IServiceAccessValidator serviceAccessValidator;
        private readonly IUserStore userStore;
        private readonly Config config;
        private readonly MessengerSender messengerSender;

        public SignInAccessValidator(IServiceAccessValidator serviceAccessValidator, IUserStore userStore, IClientSignInCaching clientSignInCaching, Config config, MessengerSender messengerSender)
        {
            this.serviceAccessValidator = serviceAccessValidator;
            this.userStore = userStore;
            this.config = config;
            this.messengerSender = messengerSender;
            clientSignInCaching.OnOffline += (SignInCacheInfo cache) =>
            {
                if (GetUser(cache.Args, out UserInfo user))
                {
                    user.Connections.TryRemove(cache.ConnectionId, out _);
                }
            };
        }

        public EnumSignInValidatorOrder Order => EnumSignInValidatorOrder.None;
        public uint Access => (uint)EnumServiceAccess.SignIn;

        public string Name => "登入";

        public SignInResultInfo.SignInResultInfoCodes Validate(Dictionary<string, string> args, ref uint access)
        {
            if(GetUser(args, out UserInfo user))
            {
                access |= user.Access;
            }
            if (config.Enable)
            {
                if (user == null)
                    return SignInResultInfo.SignInResultInfoCodes.NOT_FOUND;

                //该账号不允许登入
                if (serviceAccessValidator.Validate(access, Access) == false)
                {
                    return SignInResultInfo.SignInResultInfoCodes.ENABLE;
                }
                //账号过期了
                if (user.EndTime <= DateTime.Now)
                {
                    return SignInResultInfo.SignInResultInfoCodes.TIME_OUT_RANGE;
                }
                //超过登录数量
                if (user.SignLimit > -1)
                {
                    if (user.Connections.Count >= user.SignLimit)
                    {
                        if (config.ForceOffline)
                        {
                            foreach (var item in user.Connections)
                            {
                                _ = messengerSender.SendOnly(new MessageRequestWrap
                                {
                                    Connection = item.Value,
                                    MessengerId = (ushort)ClientsMessengerIds.Exit,
                                });
                            }
                            user.Connections.Clear();
                        }
                        else
                        {
                            return SignInResultInfo.SignInResultInfoCodes.LIMIT_OUT_RANGE;
                        }
                    }
                }

            }
            return SignInResultInfo.SignInResultInfoCodes.OK;
        }

        public void Validated(SignInCacheInfo cache)
        {
            if (GetUser(cache.Args, out UserInfo user))
            {
                user.Connections.TryAdd(cache.ConnectionId, cache.Connection);
            }
        }

        private bool GetUser(Dictionary<string, string> args, out UserInfo user)
        {
            user = default;
            if (GetAccountPassword(args, out string account, out string password))
            {
                return userStore.Get(account, password, out user);
            }
            return false;
        }
        public static bool GetAccountPassword(Dictionary<string, string> args, out string account, out string password)
        {
            bool res = args.TryGetValue("account", out account);
            bool res1 = args.TryGetValue("password", out password);
            return res && res1;
        }
    }


}
