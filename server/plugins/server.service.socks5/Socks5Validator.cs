using common.socks5;
using server.messengers;
using common.server;
using server.messengers.singnin;
using common.server.model;

namespace server.service.socks5
{
    public sealed class Socks5Validator : ISocks5Validator
    {
        private readonly IServiceAccessValidator serviceAccessProvider;
        private readonly common.socks5.Config config;
        private readonly IClientSignInCaching clientSignInCaching;

        public Socks5Validator(IServiceAccessValidator serviceAccessProvider, common.socks5.Config config, IClientSignInCaching clientSignInCaching)
        {
            this.serviceAccessProvider = serviceAccessProvider;
            this.config = config;
            this.clientSignInCaching = clientSignInCaching;
        }

        public bool Validate(Socks5Info info)
        {
            if (Socks5Parser.GetIsLanAddress(info.Data))
            {
                return false;
            }

            if (clientSignInCaching.Get(info.ClientId, out SignInCacheInfo client))
            {
                return config.ConnectEnable || serviceAccessProvider.Validate(info.ClientId, EnumServiceAccess.Socks5);
            }

            return config.ConnectEnable;
        }
    }

}
