using common.server;

namespace server.messengers.register
{
    public interface IRelayValidator
    {
        public bool Validate(string key);
    }

    public class DefaultRelayValidator : IRelayValidator
    {
        public DefaultRelayValidator()
        {
        }
        public bool Validate(string key)
        {
            return true;
        }
    }

}
