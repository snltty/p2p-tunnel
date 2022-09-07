using common.socks5;

namespace client.service.vea
{
    public interface IVeaSocks5ClientListener: ISocks5ClientListener
    {
    }
    public class VeaSocks5ClientListener: Socks5ClientListener, IVeaSocks5ClientListener
    {

    }
}
