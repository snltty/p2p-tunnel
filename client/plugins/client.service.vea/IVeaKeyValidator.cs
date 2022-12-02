using common.socks5;

namespace client.service.vea
{
    public interface IVeaKeyValidator : ISocks5Validator
    {
    }

    public sealed class DefaultVeaKeyValidator : DefaultSocks5Validator, IVeaKeyValidator
    {
        public DefaultVeaKeyValidator(Config config) : base(new common.socks5.Config
        {
            BufferSize = config.BufferSize,
            ConnectEnable = config.ConnectEnable,
            NumConnections = config.NumConnections,
        })
        {

        }
    }

}
