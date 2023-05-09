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

        public EnumSignInValidatorOrder Order => EnumSignInValidatorOrder.Level9;

        public uint Access => (uint)server.messengers.EnumServiceAccess.Relay;

        public string Name => "中继";

        public RelayValidator(Config config, IServiceAccessValidator serviceAccessProvider)
        {
            this.config = config;
            this.serviceAccessProvider = serviceAccessProvider;
        }
        public bool Validate(IConnection connection)
        {
            return config.RelayEnable || serviceAccessProvider.Validate(connection, Access);
        }

        public SignInResultInfo.SignInResultInfoCodes Validate(Dictionary<string, string> args, ref uint access)
        {
            access |= (config.RelayEnable ? Access : (uint)common.server.EnumServiceAccess.None);
            return SignInResultInfo.SignInResultInfoCodes.OK;
        }

        public void Validated(SignInCacheInfo cache)
        {

        }
    }


    public sealed class SettingValidator : ISignInValidator
    {
        public EnumSignInValidatorOrder Order => EnumSignInValidatorOrder.Level9;
        public uint Access => (uint)common.server.EnumServiceAccess.Setting;

        public string Name => "服务器配置";

        public SettingValidator()
        {
        }

        public SignInResultInfo.SignInResultInfoCodes Validate(Dictionary<string, string> args, ref uint access)
        {
            return SignInResultInfo.SignInResultInfoCodes.OK;
        }

        public void Validated(SignInCacheInfo cache)
        {
        }
    }
}
