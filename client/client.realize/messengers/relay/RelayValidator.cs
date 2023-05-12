using common.server;

namespace client.realize.messengers.relay
{
    public sealed class RelayValidator : IRelayValidator
    {
        private readonly Config config;
        private readonly IServiceAccessValidator serviceAccessValidator;

        public RelayValidator(Config config, IServiceAccessValidator serviceAccessValidator)
        {
            this.config = config;
            this.serviceAccessValidator = serviceAccessValidator;
        }
        public bool Validate(IConnection connection)
        {
            return config.Client.UseRelay || serviceAccessValidator.Validate(connection.ConnectId,(uint)EnumServiceAccess.Relay);
        }
    }
}
