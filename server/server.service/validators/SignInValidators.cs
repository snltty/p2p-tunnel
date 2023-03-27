using server.messengers.singnin;
using server.messengers;
using common.server.model;
using System;

namespace server.service.validators
{
    /// <summary>
    /// 登入权限验证
    /// </summary>
    public sealed class SignInAccessMiddleware : ISignInValidator, ISignInAccess
    {
        private readonly IServiceAccessValidator serviceAccessValidator;
        public SignInAccessMiddleware(IServiceAccessValidator serviceAccessValidator)
        {
            this.serviceAccessValidator = serviceAccessValidator;
        }

        public EnumServiceAccess Access()
        {
            return EnumServiceAccess.None;
        }

        public SignInResultInfo.SignInResultInfoCodes Validate(UserInfo user)
        {
            //该账号不允许登入
            if (serviceAccessValidator.Validate(user.Access, EnumServiceAccess.SignIn) == false)
            {
                return SignInResultInfo.SignInResultInfoCodes.ENABLE;
            }
            return SignInResultInfo.SignInResultInfoCodes.OK;
        }
    }

    /// <summary>
    /// 中继验证
    /// </summary>
    public sealed class SignInRelayMiddleware : ISignInValidator, ISignInAccess
    {
        private readonly Config config;
        public SignInRelayMiddleware(Config config)
        {
            this.config = config;
        }

        public EnumServiceAccess Access()
        {
            return config.RelayEnable ? EnumServiceAccess.Relay : EnumServiceAccess.None;
        }
        public SignInResultInfo.SignInResultInfoCodes Validate(UserInfo user)
        {
            return SignInResultInfo.SignInResultInfoCodes.OK;
        }
    }

    /// <summary>
    /// 账号过期时间验证
    /// </summary>
    public sealed class SignInEndTimeMiddleware : ISignInValidator, ISignInAccess
    {
        public EnumServiceAccess Access()
        {
            return EnumServiceAccess.None;
        }
        public SignInResultInfo.SignInResultInfoCodes Validate(UserInfo user)
        {
            //账号过期了
            if (user.EndTime <= DateTime.Now)
            {
                return SignInResultInfo.SignInResultInfoCodes.TIME_OUT_RANGE;
            }
            return SignInResultInfo.SignInResultInfoCodes.OK;
        }

    }

    /// <summary>
    /// 流量剩余验证
    /// </summary>
    public sealed class SignInNetFlowMiddleware : ISignInValidator, ISignInAccess
    {
        public EnumServiceAccess Access()
        {
            return EnumServiceAccess.None;
        }
        public SignInResultInfo.SignInResultInfoCodes Validate(UserInfo user)
        {
            if (user.NetFlow == 0)
            {
                return SignInResultInfo.SignInResultInfoCodes.NETFLOW_OUT_RANGE;
            }
            return SignInResultInfo.SignInResultInfoCodes.OK;
        }
    }

    /// <summary>
    /// 登入数量验证
    /// </summary>
    public sealed class SignInSignLimitMiddleware : ISignInValidator, ISignInAccess
    {
        private readonly IClientSignInCaching clientSignInCaching;
        public SignInSignLimitMiddleware(IClientSignInCaching clientSignInCaching)
        {
            this.clientSignInCaching = clientSignInCaching;
        }

        public EnumServiceAccess Access()
        {
            return EnumServiceAccess.None;
        }
        public SignInResultInfo.SignInResultInfoCodes Validate(UserInfo user)
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
