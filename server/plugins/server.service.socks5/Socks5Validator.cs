using common.socks5;
using server.messengers;
using server.messengers.singnin;
using common.server.model;
using System.Collections.Generic;

namespace server.service.socks5
{
    public sealed class Socks5Validator :  ISignInValidator
    {
        
        private readonly common.socks5.Config config;
       

        public EnumSignInValidatorOrder Order => EnumSignInValidatorOrder.Level9;
        public uint Access => 0b00000000_00000000_00000000_00001000;
        public string Name => "socks5";

        public Socks5Validator( common.socks5.Config config)
        {
            this.config = config;
        }

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
