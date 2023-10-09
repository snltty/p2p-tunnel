using server.messengers.signin;

namespace server.service.validators
{
    public sealed class ServiceAccessValidator : common.server.ServiceAccessValidator
    {
        private readonly IClientSignInCaching clientSignInCaching;
        public ServiceAccessValidator(IClientSignInCaching clientSignInCaching)
        {
            this.clientSignInCaching = clientSignInCaching;
        }

        public override bool Validate(ulong connectionid, uint service)
        {
            if (clientSignInCaching.Get(connectionid, out SignInCacheInfo user))
            {
                return Validate(user.UserAccess, service);
            }
            return false;
        }
    }
}
