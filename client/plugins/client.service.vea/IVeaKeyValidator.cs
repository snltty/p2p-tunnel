using common.socks5;

namespace client.service.vea
{
    public interface IVeaKeyValidator : ISocks5Validator
    {
    }

    public class DefaultVeaKeyValidator : DefaultSocks5Validator, IVeaKeyValidator
    {

    }

}
