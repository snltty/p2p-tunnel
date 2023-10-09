using client.messengers.clients;
using client.messengers.signin;

namespace client.realize.messengers.clients
{
    public sealed class ServiceAccessValidator : common.server.ServiceAccessValidator
    {
        private readonly IClientInfoCaching clientInfoCaching;
        private readonly SignInStateInfo signInStateInfo;
        public ServiceAccessValidator(IClientInfoCaching clientInfoCaching, SignInStateInfo signInStateInfo)
        {
            this.clientInfoCaching = clientInfoCaching;
            this.signInStateInfo = signInStateInfo;
        }

        public override bool Validate(ulong connectionid, uint service)
        {
            if (signInStateInfo.ConnectId == connectionid) return true;

            if (clientInfoCaching.Get(connectionid, out ClientInfo user))
            {
                return Validate(user.UserAccess, service);
            }
            return false;
        }
    }
}
