using server.messengers.singnin;
using server.messengers;
using common.server.model;

namespace server.service.validators
{
    public sealed class SignInValidator : ISignInValidator
    {
        private readonly Config config;
        private readonly IServiceAccessValidator serviceAccessProvider;
        public SignInValidator(Config config, IServiceAccessValidator serviceAccessProvider)
        {
            this.config = config;
            this.serviceAccessProvider = serviceAccessProvider;
        }
        public bool Validate(string groupid)
        {
            return config.RegisterEnable || serviceAccessProvider.Validate(groupid, EnumServiceAccess.SignIn);
        }
    }


}
