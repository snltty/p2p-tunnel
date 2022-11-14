using common.server;

namespace client.realize.messengers.relay
{
    public class RelayValidator : IRelayValidator
    {
        private readonly Config config;
        public RelayValidator(Config config)
        {
            this.config = config;
        }
        public bool Validate(IConnection connection)
        {
            return config.Client.UseRelay;

        }
    }
}
