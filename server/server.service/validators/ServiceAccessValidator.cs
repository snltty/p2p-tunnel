using common.server;
using common.server.model;
using server.messengers;
using server.messengers.singnin;

namespace server.service.validators
{
    public sealed class ServiceAccessValidator : IServiceAccessValidator
    {
        private readonly IClientSignInCaching clientSignInCaching;
        public ServiceAccessValidator(IClientSignInCaching clientSignInCaching)
        {
            this.clientSignInCaching = clientSignInCaching;
        }

        public bool Validate(IConnection connection, uint service)
        {
            return Validate(connection.ConnectId, service);
        }
        public bool Validate(ulong connectionid, uint service)
        {
            if (clientSignInCaching.Get(connectionid, out SignInCacheInfo user))
            {
                return Validate(user, service);
            }
            return false;
        }
        public bool Validate(SignInCacheInfo cache, uint service)
        {
            if (cache == null) return false;
            return Validate(cache.UserAccess, service);
        }
        public bool Validate(uint access, uint service)
        {
            return (access & service) == service;
        }
    }
}
