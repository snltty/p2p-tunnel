using server.messengers.register;
using server.messengers;

namespace server.service.validators
{
    public class RelayValidator : IRelayValidator
    {

        private readonly Config config;
        private readonly IServiceAccessValidator serviceAccessProvider;
        public RelayValidator(Config config, IServiceAccessValidator serviceAccessProvider)
        {
            this.config = config;
            this.serviceAccessProvider = serviceAccessProvider;
        }
        public bool Validate(string key)
        {
            return config.RelayEnable || serviceAccessProvider.Validate(key, EnumService.Relay);
        }
    }
}
