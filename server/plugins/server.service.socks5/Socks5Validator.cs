using server.messengers.singnin;
using common.server.model;
using System.Collections.Generic;

namespace server.service.socks5
{
    public sealed class Socks5Validator : ISignInValidator
    {

        private readonly common.socks5.Config config;
        private readonly IServerSocks5ProxyPlugin serverSocks5ProxyPlugin;


        public EnumSignInValidatorOrder Order => EnumSignInValidatorOrder.Level9;

        public Socks5Validator(common.socks5.Config config, IServerSocks5ProxyPlugin serverSocks5ProxyPlugin)
        {
            this.config = config;
            this.serverSocks5ProxyPlugin = serverSocks5ProxyPlugin;
        }

        public SignInResultInfo.SignInResultInfoCodes Validate(Dictionary<string, string> args, ref uint access)
        {
            access |= (config.ConnectEnable ? serverSocks5ProxyPlugin.Access : (uint)common.server.EnumServiceAccess.None);
            return SignInResultInfo.SignInResultInfoCodes.OK;
        }
        public void Validated(SignInCacheInfo cache)
        {
        }

    }
}
