using System.Net;
using System.Threading.Tasks;

namespace common.socks5
{
    /// <summary>
    /// 
    /// </summary>
    public interface ISocks5ClientHandler
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        Task InputData(Socks5Info info);
    }



    public interface ISocks5DstEndpointProvider
    {
        public IPEndPoint Get(ushort listenPort);
    }
    public class Socks5DstEndpointProvider : ISocks5DstEndpointProvider
    {
        private IPEndPoint point = new IPEndPoint(IPAddress.Any, 0);
        public IPEndPoint Get(ushort listenPort)
        {
            point.Port = listenPort;
            return point;
        }
    }
}
