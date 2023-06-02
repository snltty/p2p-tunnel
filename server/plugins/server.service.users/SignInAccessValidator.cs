using common.libs;
using common.server;
using common.server.model;
using common.user;
using server.messengers.singnin;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace server.service.users
{
    /// <summary>
    /// 登入权限验证
    /// </summary>
    public sealed class SignInAccessValidator : ISignInValidator, IUserInfoCaching, IAccess
    {
        public ConcurrentDictionary<ulong, UserInfo> Connections { get; set; } = new ConcurrentDictionary<ulong, UserInfo>();
        private readonly IServiceAccessValidator serviceAccessValidator;
        private readonly IUserStore userStore;
        private readonly common.user.Config config;
        private readonly MessengerSender messengerSender;
        private readonly IClientSignInCaching clientSignInCaching;

        private const string useridKey = "UserInfoID";

        public SignInAccessValidator(IServiceAccessValidator serviceAccessValidator, IUserStore userStore, IClientSignInCaching clientSignInCaching, common.user.Config config, MessengerSender messengerSender, WheelTimer<object> wheelTimer)
        {
            this.serviceAccessValidator = serviceAccessValidator;
            this.userStore = userStore;
            this.config = config;
            this.messengerSender = messengerSender;
            this.clientSignInCaching = clientSignInCaching;
            clientSignInCaching.OnOffline += (SignInCacheInfo cache) =>
            {
                if (GetUser(cache.Args, out UserInfo user))
                {
                    user.Connections.TryRemove(cache.ConnectionId, out _);
                }
            };
            wheelTimer.NewTimeout(new WheelTimerTimeoutTask<object>
            {
                Callback = NetFlowTimeout
            }, 5000, true);
        }

        public EnumSignInValidatorOrder Order => EnumSignInValidatorOrder.None;
        public uint Access => (uint)messengers.EnumServiceAccess.SignIn;
        public string Name => "sign in";

        public SignInResultInfo.SignInResultInfoCodes Validate(Dictionary<string, string> args, ref uint access)
        {
            if (GetUser(args, out UserInfo user))
            {
                //该账号不允许登入
                if (serviceAccessValidator.Validate(user.Access, Access) == false)
                {
                    return SignInResultInfo.SignInResultInfoCodes.ENABLE;
                }
                //账号过期了
                if (user.EndTime <= DateTime.Now)
                {
                    return SignInResultInfo.SignInResultInfoCodes.TIME_OUT_RANGE;
                }
                //超过登录数量
                if (user.SignLimitDenied)
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
                access |= user.Access;
                args[useridKey] = user.ID.ToString();
            }

            if (config.Enable && user == null)
            {
                return SignInResultInfo.SignInResultInfoCodes.NOT_FOUND;
            }
            return SignInResultInfo.SignInResultInfoCodes.OK;
        }
        public void Validated(SignInCacheInfo cache)
        {
            if (GetUser(cache.Args, out UserInfo user))
            {
                if (user.NetFlowDenied)
                {
                    cache.Connection.SendDenied |= config.NetFlowBit;
                }
                user.Connections.TryAdd(cache.ConnectionId, cache.Connection);
            }
        }

        public bool GetUser(IConnection connection, out UserInfo user)
        {
            user = default;
            if (clientSignInCaching.Get(connection.ConnectId, out SignInCacheInfo sign))
            {
                return GetUser(sign.Args, out user);
            }
            return false;
        }
        private bool GetUser(Dictionary<string, string> args, out UserInfo user)
        {
            user = default;
            if (args.TryGetValue(useridKey, out string uid))
            {
                return userStore.Get(ulong.Parse(uid), out user);
            }
            else if (GetAccountPassword(args, out string account, out string password))
            {
                return userStore.Get(account, password, out user);
            }
            return false;
        }
        private bool GetAccountPassword(Dictionary<string, string> args, out string account, out string password)
        {
            bool res = args.TryGetValue("account", out account);
            bool res1 = args.TryGetValue("password", out password);
            return res && res1;
        }

        /// <summary>
        /// 定时检查和保存
        /// </summary>
        /// <param name="timeout"></param>
        private void NetFlowTimeout(WheelTimerTimeout<object> timeout)
        {
            try
            {
                List<SignInCacheInfo> caches = clientSignInCaching.Get();
                foreach (SignInCacheInfo item in caches.Where(c => c.Connection != null && c.Connection.Connected))
                {
                    if (GetUser(item.Args, out UserInfo user))
                    {
                        ulong netflow = (item.Connection.SentBytes - user.LastSentBytes);
                        user.SentBytes += netflow;

                        user.NetFlow -= (long)netflow;
                        if (user.NetFlow < 0)
                        {
                            user.NetFlow = 0;
                        }

                        user.LastSentBytes = item.Connection.SentBytes;

                        foreach (var connection in user.Connections)
                        {
                            if (user.NetFlowDenied)
                            {
                                connection.Value.SendDenied |= config.NetFlowBit;
                            }
                            else
                            {
                                connection.Value.SendDenied &= (byte)(~config.NetFlowBit);
                            }
                        }

                    }
                }
                userStore.Save();
            }
            catch (Exception)
            {
            }
        }
    }


}
