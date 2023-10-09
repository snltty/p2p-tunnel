using common.server;
using server.messengers.signin;
using common.server.model;
using System.Collections.Generic;

namespace server.service.validators
{
    public sealed class RelayValidator : IRelayValidator, ISignInValidator, IAccess
    {

        private readonly Config config;
        private readonly IServiceAccessValidator serviceAccessProvider;

        public EnumSignInValidatorOrder Order => EnumSignInValidatorOrder.Level9;

        public uint Access => (uint)EnumServiceAccess.Relay;
        public string Name => "relay";

        public RelayValidator(Config config, IServiceAccessValidator serviceAccessProvider)
        {
            this.config = config;
            this.serviceAccessProvider = serviceAccessProvider;
        }
        public bool Validate(IConnection connection)
        {
            return config.RelayEnable || serviceAccessProvider.Validate(connection.ConnectId, Access);
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


    public sealed class SettingValidator : ISignInValidator, IAccess
    {
        public EnumSignInValidatorOrder Order => EnumSignInValidatorOrder.Level9;
        public uint Access => (uint)EnumServiceAccess.Setting;

        public string Name => "setting";

        public SignInResultInfo.SignInResultInfoCodes Validate(Dictionary<string, string> args, ref uint access)
        {
            return SignInResultInfo.SignInResultInfoCodes.OK;
        }

        public void Validated(SignInCacheInfo cache)
        {
        }
    }
}
