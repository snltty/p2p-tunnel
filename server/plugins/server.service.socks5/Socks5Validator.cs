using common.socks5;
using server.messengers;
using server.messengers.singnin;
using common.server.model;
using System.Collections.Generic;

namespace server.service.socks5
{
    public sealed class Socks5Validator : ISocks5Validator, ISignInValidator
    {
        private readonly IServiceAccessValidator serviceAccessProvider;
        private readonly common.socks5.Config config;
        private readonly IClientSignInCaching clientSignInCaching;

        public EnumSignInValidatorOrder Order => EnumSignInValidatorOrder.Level9;
        public uint Access => 0b00000000_00000000_00000000_00000100;

        public Socks5Validator(IServiceAccessValidator serviceAccessProvider, common.socks5.Config config, IClientSignInCaching clientSignInCaching)
        {
            this.serviceAccessProvider = serviceAccessProvider;
            this.config = config;
            this.clientSignInCaching = clientSignInCaching;
        }

        public SignInResultInfo.SignInResultInfoCodes Validate(Dictionary<string, string> args, ref uint access)
        {
            access |= (config.ConnectEnable ? Access : (uint)EnumServiceAccess.None);
            return SignInResultInfo.SignInResultInfoCodes.OK;
        }
        public void Validated(SignInCacheInfo cache)
        {
        }

        public bool Validate(Socks5Info info)
        {
            if (Socks5Parser.GetIsLanAddress(info.Data))
            {
                return false;
            }

            if (clientSignInCaching.Get(info.ClientId, out SignInCacheInfo client))
            {
                return config.ConnectEnable || serviceAccessProvider.Validate(client.UserAccess, Access);
            }

            return config.ConnectEnable;
        }

       
    }
}
