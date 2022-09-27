using common.socks5;

namespace client.service.vea
{
    public interface IVeaKeyValidator : ISocks5KeyValidator
    {
    }

    public class DefaultVeaKeyValidator : DefaultSocks5KeyValidator, IVeaKeyValidator
    {

    }

}
