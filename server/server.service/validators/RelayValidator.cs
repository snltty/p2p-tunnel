using server.messengers;
using common.server;
using server.messengers.singnin;
using common.server.model;

namespace server.service.validators
{
    public sealed class RelayValidator : IRelayValidator
    {

        private readonly Config config;
        private readonly IServiceAccessValidator serviceAccessProvider;
        private readonly IClientSignInCaching clientSignInCache;
        public RelayValidator(Config config, IServiceAccessValidator serviceAccessProvider, IClientSignInCaching clientSignInCache)
        {
            this.config = config;
            this.serviceAccessProvider = serviceAccessProvider;
            this.clientSignInCache = clientSignInCache;
        }
        public bool Validate(IConnection connection)
        {
            if (clientSignInCache.Get(connection.ConnectId, out SignInCacheInfo source))
            {
                return config.RelayEnable || serviceAccessProvider.Validate(source, EnumServiceAccess.Relay);
            }

            return false;

        }
    }
}
