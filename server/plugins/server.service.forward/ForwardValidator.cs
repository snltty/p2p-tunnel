using common.forward;
using common.server.model;
using server.messengers.signin;
using System.Collections.Generic;

namespace server.service.forward
{

    public sealed class ForwardValidator : ISignInValidator
    {
        private readonly common.forward.Config config;
        private readonly IForwardProxyPlugin forwardProxyPlugin;

        public ForwardValidator(common.forward.Config config, IForwardProxyPlugin forwardProxyPlugin)
        {
            this.config = config;
            this.forwardProxyPlugin = forwardProxyPlugin;
        }

        public EnumSignInValidatorOrder Order => EnumSignInValidatorOrder.Level9;


        public SignInResultInfo.SignInResultInfoCodes Validate(Dictionary<string, string> args, ref uint access)
        {
            access |= (config.ConnectEnable ? forwardProxyPlugin.Access : (uint)common.server.EnumServiceAccess.None);
            return SignInResultInfo.SignInResultInfoCodes.OK;
        }
        public void Validated(SignInCacheInfo cache)
        {

        }
    }

}
