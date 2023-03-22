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
        private readonly IClientSignInCaching clientSignInCaching;

        public SignInValidator(Config config, IServiceAccessValidator serviceAccessProvider, IUserStore userStore, IClientSignInCaching clientSignInCaching)
        {
            this.config = config;
            this.serviceAccessProvider = serviceAccessProvider;
            this.userStore = userStore;
            this.clientSignInCaching = clientSignInCaching;
        }
        public SignInResultInfo.SignInResultInfoCodes Validate(ref UserInfo user)
        {
            if (config.RegisterEnable == false)
            {
                return SignInResultInfo.SignInResultInfoCodes.SIGNIN_VERIFY;
            }

            //验证账号
            if (config.VerifyAccount)
            {
                if (user == null)
                {
                    return SignInResultInfo.SignInResultInfoCodes.NOT_FOUND;
                }
                //该账号不允许登入
                if (serviceAccessProvider.Validate(user.Access, EnumServiceAccess.SignIn) == false)
                {
                    return SignInResultInfo.SignInResultInfoCodes.SIGNIN_VERIFY;
                }
                //账号到期
                if (user.EndTime <= DateTime.Now)
                {
                    return SignInResultInfo.SignInResultInfoCodes.TIME_OUT_RANGE;
                }
                //流量为0
                if (user.NetFlow == 0)
                {
                    return SignInResultInfo.SignInResultInfoCodes.NETFLOW_OUT_RANGE;
                }
                //登入数量限制
                if (user.SignLimit > 0)
                {
                    if (clientSignInCaching.UserCount(user.ID) >= user.SignLimit)
                    {
                        return SignInResultInfo.SignInResultInfoCodes.LIMIT_OUT_RANGE;
                    }
                }
            }
            else
            {
                //不验证账号，给个游客账号
                user = userStore.DefaultUser;
            }

            return SignInResultInfo.SignInResultInfoCodes.OK;
        }
    }

}
