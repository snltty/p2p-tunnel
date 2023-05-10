using common.server.model;
using server.messengers.singnin;
using Microsoft.Extensions.DependencyInjection;
using common.libs;
using System.Reflection;
using System.Linq;
using System;
using common.proxy;

namespace server.service.validators
{
    public sealed class SignInValidatorHandler : ISignInValidatorHandler
    {
        Wrap<ISignInValidator> first;
        Wrap<ISignInValidator> last;

        private readonly Config config;
        private readonly IClientSignInCaching clientSignInCache;
        private readonly ServiceProvider serviceProvider;
        public SignInValidatorHandler(Config config, IClientSignInCaching clientSignInCache, ServiceProvider serviceProvider)
        {
            this.config = config;
            this.clientSignInCache = clientSignInCache;
            this.serviceProvider = serviceProvider;
        }

        public void LoadValidator(Assembly[] assemblys)
        {
            foreach (ISignInValidator validator in ReflectionHelper.GetInterfaceSchieves(assemblys, typeof(ISignInValidator)).Distinct().Select(c => (ISignInValidator)serviceProvider.GetService(c)).OrderBy(c => c.Order))
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
        }

        public SignInResultInfo.SignInResultInfoCodes Validate(SignInParamsInfo model, ref uint access)
        {
            //未开启登入，且不是管理员分组
            if (config.RegisterEnable == false && (string.IsNullOrWhiteSpace(config.AdminGroup) || model.GroupId != config.AdminGroup))
            {
                return SignInResultInfo.SignInResultInfoCodes.ENABLE;
            }

            //重名
            if (clientSignInCache.Get(model.ConnectionId, out SignInCacheInfo client))
            {
                clientSignInCache.Remove(client.ConnectionId);
            }


            //是管理员分组的
            if (string.IsNullOrWhiteSpace(config.AdminGroup) == false && model.GroupId == config.AdminGroup)
            {
                access |= (uint)common.server.EnumServiceAccess.All;
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
