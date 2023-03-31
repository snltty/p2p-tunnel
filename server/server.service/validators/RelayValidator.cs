using server.messengers;
using common.server;
using server.messengers.singnin;
using common.server.model;
using System.Collections.Generic;

namespace server.service.validators
{
    public sealed class RelayValidator : IRelayValidator, ISignInValidator
    {

        private readonly Config config;
        private readonly IServiceAccessValidator serviceAccessProvider;
        private readonly IClientSignInCaching clientSignInCache;

        public EnumSignInValidatorOrder Order => EnumSignInValidatorOrder.Level9;

        public uint Access => (uint)EnumServiceAccess.Relay;

        public RelayValidator(Config config, IServiceAccessValidator serviceAccessProvider, IClientSignInCaching clientSignInCache)
        {
            this.config = config;
            this.serviceAccessProvider = serviceAccessProvider;
            this.clientSignInCache = clientSignInCache;
        }
        public bool Validate(IConnection connection)
        {
            if (clientSignInCache.Get(connection.ConnectId, out SignInCacheInfo source))
            {
                return config.RelayEnable || serviceAccessProvider.Validate(source, Access);
            }

            return false;

        }

        public SignInResultInfo.SignInResultInfoCodes Validate(Dictionary<string, string> args, ref uint access)
        {
            access |= (config.RelayEnable ? Access : (uint)EnumServiceAccess.None);
            return SignInResultInfo.SignInResultInfoCodes.OK;
        }

        public void Validated(SignInCacheInfo cache)
        {

        }
    }
}
