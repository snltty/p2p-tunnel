using server.messengers.register;
using server.messengers;
using common.server.model;

namespace server.service.validators
{
    public sealed class RegisterValidator : IRegisterKeyValidator
    {
        private readonly Config config;
        private readonly IServiceAccessValidator serviceAccessProvider;
        public RegisterValidator(Config config, IServiceAccessValidator serviceAccessProvider)
        {
            this.config = config;
            this.serviceAccessProvider = serviceAccessProvider;
        }
        public bool Validate(string key)
        {
            return config.RegisterEnable || serviceAccessProvider.Validate(key, EnumServiceAccess.Register);
        }
    }


}
