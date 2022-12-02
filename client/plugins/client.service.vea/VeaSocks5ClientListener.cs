using common.socks5;

namespace client.service.vea
{
    public interface IVeaSocks5ClientListener: ISocks5ClientListener
    {
    }
    public sealed class VeaSocks5ClientListener: Socks5ClientListener, IVeaSocks5ClientListener
    {

    }
}
