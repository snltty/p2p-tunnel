using server.messengers;
using common.server;
using server.messengers.register;
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
        private readonly IClientRegisterCaching clientRegisterCache;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        /// <param name="serviceAccessProvider"></param>
        /// <param name="clientRegisterCache"></param>
        public RelayValidator(Config config, IServiceAccessValidator serviceAccessProvider, IClientRegisterCaching clientRegisterCache)
        {
            this.config = config;
            this.serviceAccessProvider = serviceAccessProvider;
            this.clientRegisterCache = clientRegisterCache;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
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
