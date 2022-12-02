using common.server;

namespace common.server
{
    public interface IRelayValidator
    {
        public bool Validate(IConnection connection);
    }

    public sealed class DefaultRelayValidator : IRelayValidator
    {
        public DefaultRelayValidator()
        {
        }
        public bool Validate(IConnection connection)
        {
            return true;
        }
    }

}
