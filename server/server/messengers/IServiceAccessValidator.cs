using common.server;
using common.server.model;
using server.messengers.singnin;

namespace server.messengers
{
    public interface IServiceAccessValidator
    {
        public bool Validate(IConnection connection, EnumServiceAccess service);
        public bool Validate(ulong connectionid, EnumServiceAccess service);
        public bool Validate(SignInCacheInfo cache, EnumServiceAccess service);
        public bool Validate(uint access, EnumServiceAccess service);
    }

}
