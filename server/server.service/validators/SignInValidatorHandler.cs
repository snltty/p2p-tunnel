using common.server.model;
using server.messengers.singnin;
using server.messengers;

namespace server.service.validators
{
    public sealed class SignInValidatorHandler : ISignInValidatorHandler
    {
        Wrap<ISignInValidator> first;
        Wrap<ISignInValidator> last;

        Wrap<ISignInAccess> firstAccess;
        Wrap<ISignInAccess> lastAccess;

        private readonly Config config;
        private readonly IUserStore userStore;
        public SignInValidatorHandler(Config config, IUserStore userStore)
        {
            this.config = config;
            this.userStore = userStore;
        }

        public void LoadValidator(ISignInValidator validator)
        {
            if (first == null)
            {
                first = new Wrap<ISignInValidator> { Value = validator };
                last = first;
            }
            else
            {
                last.Next = new Wrap<ISignInValidator> { Value = validator };
                last = last.Next;
            }
        }
        public void LoadAccess(ISignInAccess access)
        {
            if (firstAccess == null)
            {
                firstAccess = new Wrap<ISignInAccess> { Value = access };
                lastAccess = firstAccess;
            }
            else
            {
                lastAccess.Next = new Wrap<ISignInAccess> { Value = access };
                lastAccess = lastAccess.Next;
            }
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
                Wrap<ISignInValidator> current = first;
                while (current != null)
                {
                    SignInResultInfo.SignInResultInfoCodes code = current.Value.Validate(user);
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
                    user = userStore.DefaultUser();
                }
            }

            return SignInResultInfo.SignInResultInfoCodes.OK;
        }

        public EnumServiceAccess Access()
        {
            EnumServiceAccess enumServiceAccess = EnumServiceAccess.None;
            Wrap<ISignInAccess> current = firstAccess;
            while (current != null)
            {
                enumServiceAccess |= current.Value.Access();
                current = current.Next;
            }
            return enumServiceAccess;
        }


        class Wrap<T>
        {
            public T Value { get; set; }
            public Wrap<T> Next { get; set; }
        }
    }
}
