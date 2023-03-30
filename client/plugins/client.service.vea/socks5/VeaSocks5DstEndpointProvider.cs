using common.socks5;
using System.Net;

namespace client.service.vea.socks5
{
    public interface IVeaSocks5DstEndpointProvider : ISocks5DstEndpointProvider
    {

    }

    public sealed class VeaSocks5DstEndpointProvider : Socks5DstEndpointProvider, IVeaSocks5DstEndpointProvider
    {
    }
}
