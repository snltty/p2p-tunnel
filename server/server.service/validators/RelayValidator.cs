using server.messengers;
using common.server;
using server.messengers.singnin;
using common.server.model;

namespace server.service.validators
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class RelayValidator : IRelayValidator
    {

        private readonly Config config;
        private readonly IServiceAccessValidator serviceAccessProvider;
        private readonly IClientSignInCaching clientSignInCache;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        /// <param name="serviceAccessProvider"></param>
        /// <param name="clientSignInCache"></param>
        public RelayValidator(Config config, IServiceAccessValidator serviceAccessProvider, IClientSignInCaching clientSignInCache)
        {
            this.config = config;
            this.serviceAccessProvider = serviceAccessProvider;
            this.clientSignInCache = clientSignInCache;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public bool Validate(IConnection connection)
        {
            if (clientSignInCache.Get(connection.ConnectId, out SignInCacheInfo source))
            {
                return config.RelayEnable || serviceAccessProvider.Validate(source.GroupId, EnumServiceAccess.Relay);
            }

            return false;

        }
    }
}
