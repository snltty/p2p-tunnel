using common.server;
using System;
using System.Net;

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
        /// <param name="connection"></param>
        void InputData(IConnection connection);
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
