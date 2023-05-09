using common.httpProxy;
using common.server.model;
using server.messengers;
using server.messengers.singnin;
using System.Collections.Generic;

namespace server.service.httpProxy
{

    public sealed class HttpProxyValidator : ISignInValidator
    {
        private readonly common.httpProxy.Config config;

        public HttpProxyValidator(common.httpProxy.Config config)
        {
            this.config = config;
        }

        public EnumSignInValidatorOrder Order => EnumSignInValidatorOrder.Level9;
        public uint Access => ServerHttpProxyPlugin.Access;

        public string Name => "http代理";


        public SignInResultInfo.SignInResultInfoCodes Validate(Dictionary<string, string> args, ref uint access)
        {
            access |= (config.ConnectEnable ? Access : (uint)common.server.EnumServiceAccess.None);
            return SignInResultInfo.SignInResultInfoCodes.OK;
        }
        public void Validated(SignInCacheInfo cache)
        {

        }
    }

}
