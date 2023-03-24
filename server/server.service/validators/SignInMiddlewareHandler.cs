using common.server.model;
using server.messengers.singnin;
using server.messengers;

namespace server.service.validators
{
    public class SignInMiddlewareHandler : ISignInMiddlewareHandler
    {
        SignInMiddleware first;
        SignInMiddleware last;

        private readonly Config config;
        private readonly IUserStore userStore;
        public SignInMiddlewareHandler(Config config, IUserStore userStore)
        {
            this.config = config;
            this.userStore = userStore;
        }

        public ISignInMiddlewareHandler Use(SignInMiddleware middle)
        {
            if (first == null)
            {
                first = middle;
                last = first;
            }
            else
            {
                last.Next = middle;
                last = middle;
            }
            return this;
        }

        public SignInResultInfo.SignInResultInfoCodes Validate(ref UserInfo user)
        {
            //未开启登入
            if (config.RegisterEnable == false)
            {
                return SignInResultInfo.SignInResultInfoCodes.ENABLE;
            }

            //验证账号
            if (config.VerifyAccount)
            {
                if (user == null)
                {
                    return SignInResultInfo.SignInResultInfoCodes.NOT_FOUND;
                }

                //其它自定义验证
                SignInMiddleware current = first;
                while (current != null)
                {
                    SignInResultInfo.SignInResultInfoCodes code = current.Validate(user);
                    if (code != SignInResultInfo.SignInResultInfoCodes.OK)
                    {
                        return code;
                    }
                    current = current.Next;
                }
            }
            else
            {
                if (user == null)
                {
                    //不验证账号，给个游客账号
                    user = userStore.DefaultUser;
                }
            }

            return SignInResultInfo.SignInResultInfoCodes.OK;
        }

        public void Access(SignInCacheInfo cache)
        {
            SignInMiddleware current = first;
            while (current != null)
            {
                current.Access(cache);
                current = current.Next;
            }
        }
    }
}
