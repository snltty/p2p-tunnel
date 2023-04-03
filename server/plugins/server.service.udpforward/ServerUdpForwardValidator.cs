using common.server;
using common.server.model;
using common.udpforward;
using server.messengers;
using server.messengers.singnin;
using System.Collections.Generic;

namespace server.service.udpforward
{
    public sealed class ServerUdpForwardValidator : DefaultUdpForwardValidator, IUdpForwardValidator, ISignInValidator
    {
        private readonly common.udpforward.Config config;
        private readonly IServiceAccessValidator serviceAccessProvider;

        public ServerUdpForwardValidator(common.udpforward.Config config, IServiceAccessValidator serviceAccessProvider) : base(config)
        {
            this.config = config;
            this.serviceAccessProvider = serviceAccessProvider;
        }

        public EnumSignInValidatorOrder Order => EnumSignInValidatorOrder.Level9;
        public uint Access => 0b00000000_00000000_00000000_00010000;

        public string Name => "udp代理穿透";

        public new bool Validate(IConnection connection)
        {
            return config.ConnectEnable || serviceAccessProvider.Validate(connection, Access);
        }

        public SignInResultInfo.SignInResultInfoCodes Validate(Dictionary<string, string> args, ref uint access)
        {
            access |= config.ConnectEnable ? Access : (uint)EnumServiceAccess.None;
            return SignInResultInfo.SignInResultInfoCodes.OK;
        }
        public void Validated(SignInCacheInfo cache)
        {
        }
    }

}
