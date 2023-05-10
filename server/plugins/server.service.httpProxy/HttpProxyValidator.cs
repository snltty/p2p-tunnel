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
        private readonly IServerHttpProxyPlugin serverHttpProxyPlugin;

        public HttpProxyValidator(common.httpProxy.Config config, IServerHttpProxyPlugin serverHttpProxyPlugin)
        {
            this.config = config;
            this.serverHttpProxyPlugin = serverHttpProxyPlugin;
        }

        public EnumSignInValidatorOrder Order => EnumSignInValidatorOrder.Level9;


        public SignInResultInfo.SignInResultInfoCodes Validate(Dictionary<string, string> args, ref uint access)
        {
            access |= (config.ConnectEnable ? serverHttpProxyPlugin.Access : (uint)common.server.EnumServiceAccess.None);
            return SignInResultInfo.SignInResultInfoCodes.OK;
        }
        public void Validated(SignInCacheInfo cache)
        {

        }
    }

}
