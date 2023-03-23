using server.messengers.singnin;
using server.messengers;
using common.server.model;
using System;

namespace server.service.validators
{
    public sealed class SignInValidator : ISignInValidator
    {
        private readonly Config config;
        private readonly IServiceAccessValidator serviceAccessProvider;
        private readonly IUserStore userStore;
        private readonly SignInMiddlewareHandler signInMiddlewareHandler;

        public SignInValidator(Config config, IServiceAccessValidator serviceAccessProvider, IUserStore userStore, SignInMiddlewareHandler signInMiddlewareHandler)
        {
            this.config = config;
            this.serviceAccessProvider = serviceAccessProvider;
            this.userStore = userStore;
            this.signInMiddlewareHandler = signInMiddlewareHandler;
        }
        public (SignInResultInfo.SignInResultInfoCodes, string) Validate(ref UserInfo user)
        {
            //未开启登入
            if (config.RegisterEnable == false)
            {
                return (SignInResultInfo.SignInResultInfoCodes.SIGNIN_VERIFY, string.Empty);
            }

            //验证账号
            if (config.VerifyAccount)
            {
                if (user == null)
                {
                    return (SignInResultInfo.SignInResultInfoCodes.NOT_FOUND, string.Empty);
                }
                //该账号不允许登入
                if (serviceAccessProvider.Validate(user.Access, EnumServiceAccess.SignIn) == false)
                {
                    return (SignInResultInfo.SignInResultInfoCodes.SIGNIN_VERIFY, string.Empty);
                }

                //其它自定义验证
                string msg = signInMiddlewareHandler.Execute(user);
                if (string.IsNullOrWhiteSpace(msg) == false)
                {
                    return (SignInResultInfo.SignInResultInfoCodes.UNKNOW, msg);
                }
            }
            else
            {
                //不验证账号，给个游客账号
                user = userStore.DefaultUser;
            }

            return (SignInResultInfo.SignInResultInfoCodes.OK, string.Empty);
        }
    }

    public sealed class SignInEndTimeMiddleware : SignInMiddleware
    {
        public override string Validate(UserInfo user)
        {
            if (user.EndTime <= DateTime.Now)
            {
                return "账号过期";
            }
            return string.Empty;
        }
    }
    public sealed class SignInNetFlowMiddleware : SignInMiddleware
    {
        public override string Validate(UserInfo user)
        {
            if (user.NetFlow == 0)
            {
                return "无流量剩余";
            }
            return string.Empty;
        }
    }
    public sealed class SignInSignLimitMiddleware : SignInMiddleware
    {
        private readonly IClientSignInCaching clientSignInCaching;
        public SignInSignLimitMiddleware(IClientSignInCaching clientSignInCaching)
        {
            this.clientSignInCaching = clientSignInCaching;
        }

        public override string Validate(UserInfo user)
        {
            if (user.SignLimit > 0)
            {
                if (clientSignInCaching.UserCount(user.ID) >= user.SignLimit)
                {
                    return "无登入数量剩余";
                }
            }
            return string.Empty;
        }
    }
}
