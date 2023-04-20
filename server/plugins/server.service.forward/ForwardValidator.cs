using common.server;
using common.server.model;
using server.messengers;
using server.messengers.singnin;
using server.service.forward;
using System.Collections.Generic;

namespace server.service.forward
{

    public sealed class ForwardValidator : ISignInValidator
    {
        private readonly common.forward.Config config;

        public ForwardValidator(common.forward.Config config)
        {
            this.config = config;
        }

        public EnumSignInValidatorOrder Order => EnumSignInValidatorOrder.Level9;
        public uint Access => ForwardProxyPlugin.Access;

        public string Name => "代理穿透";


        public SignInResultInfo.SignInResultInfoCodes Validate(Dictionary<string, string> args, ref uint access)
        {
            access |= (config.ConnectEnable ? Access : (uint)EnumServiceAccess.None);
            return SignInResultInfo.SignInResultInfoCodes.OK;
        }
        public void Validated(SignInCacheInfo cache)
        {

        }
    }

}
