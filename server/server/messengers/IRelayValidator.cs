using common.server;

namespace server.messengers.register
{
    public interface IRelayValidator
    {
        public bool Validate(IConnection connection, RegisterCacheInfo client);
    }

    public class DefaultRelayValidator : IRelayValidator
    {

        private readonly Config config;
        public DefaultRelayValidator(Config config)
        {
            this.config = config;
        }
        public bool Validate(IConnection connection, RegisterCacheInfo client)
        {
            return config.Relay;
        }
    }

}
