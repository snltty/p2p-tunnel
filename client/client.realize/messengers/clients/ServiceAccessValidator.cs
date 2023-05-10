using client.messengers.clients;

namespace client.realize.messengers.clients
{
    public sealed class ServiceAccessValidator : common.server.ServiceAccessValidator
    {
        private readonly IClientInfoCaching clientInfoCaching;
        public ServiceAccessValidator(IClientInfoCaching clientInfoCaching)
        {
            this.clientInfoCaching = clientInfoCaching;
        }

        public override bool Validate(ulong connectionid, uint service)
        {
            if (clientInfoCaching.Get(connectionid, out ClientInfo user))
            {
                return Validate(user.UserAccess, service);
            }
            return false;
        }
    }
}
