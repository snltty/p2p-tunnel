using server.messengers;
using common.server;
using server.messengers.register;
using common.server.model;

namespace server.service.validators
{
    public sealed class RelayValidator : IRelayValidator
    {

        private readonly Config config;
        private readonly IServiceAccessValidator serviceAccessProvider;
        private readonly IClientRegisterCaching clientRegisterCache;
        public RelayValidator(Config config, IServiceAccessValidator serviceAccessProvider, IClientRegisterCaching clientRegisterCache)
        {
            this.config = config;
            this.serviceAccessProvider = serviceAccessProvider;
            this.clientRegisterCache = clientRegisterCache;
        }
        public bool Validate(IConnection connection)
        {
            if (clientRegisterCache.Get(connection.ConnectId, out RegisterCacheInfo source))
            {
                return config.RelayEnable || serviceAccessProvider.Validate(source.GroupId, EnumServiceAccess.Relay);
            }

            return false;

        }
    }
}
