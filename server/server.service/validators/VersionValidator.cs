using server.messengers.singnin;
using common.server.model;
using System.Collections.Generic;
using common.libs;
using common.server;

namespace server.service.validators
{
    public sealed class VersionValidator : ISignInValidator, IAccess
    {
        public EnumSignInValidatorOrder Order => EnumSignInValidatorOrder.None;

        public uint Access => 0;
        public string Name => "版本判断";

        public SignInResultInfo.SignInResultInfoCodes Validate(Dictionary<string, string> args, ref uint access)
        {
            if (args.TryGetValue("version", out string version) && version == Helper.Version)
            {
                return SignInResultInfo.SignInResultInfoCodes.OK;
            }

            return SignInResultInfo.SignInResultInfoCodes.UNKNOW;
        }

        public void Validated(SignInCacheInfo cache)
        {

        }
    }

}
