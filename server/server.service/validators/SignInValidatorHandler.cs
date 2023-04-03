using common.server.model;
using server.messengers.singnin;
using server.messengers;
using common.server;
using System.Reflection;

namespace server.service.validators
{
    public sealed class SignInValidatorHandler : ISignInValidatorHandler
    {
        Wrap<ISignInValidator> first;
        Wrap<ISignInValidator> last;

        private readonly Config config;
        private readonly IClientSignInCaching clientSignInCache;
        public SignInValidatorHandler(Config config, IClientSignInCaching clientSignInCache)
        {
            this.config = config;
            this.clientSignInCache = clientSignInCache;
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

        public SignInResultInfo.SignInResultInfoCodes Validate(SignInParamsInfo model, ref uint access)
        {
            //未开启登入
            if (config.RegisterEnable == false)
            {
                return SignInResultInfo.SignInResultInfoCodes.ENABLE;
            }

            //重名
            if (clientSignInCache.Get(model.GroupId, model.Name, out SignInCacheInfo client))
            {
                return SignInResultInfo.SignInResultInfoCodes.SAME_NAMES;
            }

            //是管理员分组的
            if (string.IsNullOrWhiteSpace(config.AdminGroup) == false && model.GroupId == config.AdminGroup)
            {
                access |= (uint)EnumServiceAccess.All;
            }
            else
            {
                //验证账号
                //其它自定义验证
                Wrap<ISignInValidator> current = first;
                while (current != null)
                {
                    SignInResultInfo.SignInResultInfoCodes code = current.Value.Validate(model.Args, ref access);
                    if (code != SignInResultInfo.SignInResultInfoCodes.OK)
                    {
                        return code;
                    }
                    current = current.Next;
                }
            }

            return SignInResultInfo.SignInResultInfoCodes.OK;
        }

        public void Validated(SignInCacheInfo cache)
        {
            Wrap<ISignInValidator> current = first;
            while (current != null)
            {
                current.Value.Validated(cache);
                current = current.Next;
            }
        }

        class Wrap<T>
        {
            public T Value { get; set; }
            public Wrap<T> Next { get; set; }
        }
    }
}
