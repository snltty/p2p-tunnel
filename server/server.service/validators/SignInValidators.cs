using server.messengers.singnin;
using server.messengers;
using common.server.model;
using System;

namespace server.service.validators
{
    public sealed class SignInDefaultValidator : SignInMiddleware
    {
        private readonly IServiceAccessValidator serviceAccessProvider;

        public SignInDefaultValidator(IServiceAccessValidator serviceAccessProvider)
        {
            this.serviceAccessProvider = serviceAccessProvider;
        }

        public override void Access(SignInCacheInfo cache)
        {
        }

        public override SignInResultInfo.SignInResultInfoCodes Validate(UserInfo user)
        {
            //该账号不允许登入
            if (serviceAccessProvider.Validate(user.Access, EnumServiceAccess.SignIn) == false)
            {
                return  SignInResultInfo.SignInResultInfoCodes.ENABLE;
            }
            return SignInResultInfo.SignInResultInfoCodes.OK;
        }
    }

    public sealed class SignInRelayMiddleware : SignInMiddleware
    {
        private readonly Config config;
        public SignInRelayMiddleware(Config config)
        {
            this.config = config;
        }
        public override void Access(SignInCacheInfo cache)
        {
            //开启了全局允许中继，那就只要登入成功了就加上中继权限
            if (config.RelayEnable)
            {
                cache.UserAccess |= (uint)EnumServiceAccess.Relay;
            }
        }

        public override SignInResultInfo.SignInResultInfoCodes Validate(UserInfo user)
        {
            return SignInResultInfo.SignInResultInfoCodes.OK;
        }
    }

    public sealed class SignInEndTimeMiddleware : SignInMiddleware
    {
        public override void Access(SignInCacheInfo cache)
        {
        }

        public override SignInResultInfo.SignInResultInfoCodes Validate(UserInfo user)
        {
            if (user.EndTime <= DateTime.Now)
            {
                return SignInResultInfo.SignInResultInfoCodes.TIME_OUT_RANGE;
            }
            return SignInResultInfo.SignInResultInfoCodes.OK;
        }

    }
    public sealed class SignInNetFlowMiddleware : SignInMiddleware
    {
        public override void Access(SignInCacheInfo cache)
        {
        }
        public override SignInResultInfo.SignInResultInfoCodes Validate(UserInfo user)
        {
            if (user.NetFlow == 0)
            {
                return SignInResultInfo.SignInResultInfoCodes.NETFLOW_OUT_RANGE;
            }
            return SignInResultInfo.SignInResultInfoCodes.OK;
        }
    }
    public sealed class SignInSignLimitMiddleware : SignInMiddleware
    {
        private readonly IClientSignInCaching clientSignInCaching;
        public SignInSignLimitMiddleware(IClientSignInCaching clientSignInCaching)
        {
            this.clientSignInCaching = clientSignInCaching;
        }

        public override void Access(SignInCacheInfo cache)
        {
        }
        public override SignInResultInfo.SignInResultInfoCodes Validate(UserInfo user)
        {
            if (user.SignLimit > 0)
            {
                if (clientSignInCaching.UserCount(user.ID) >= user.SignLimit)
                {
                    return SignInResultInfo.SignInResultInfoCodes.LIMIT_OUT_RANGE;
                }
            }
            return SignInResultInfo.SignInResultInfoCodes.OK;
        }
    }
}
