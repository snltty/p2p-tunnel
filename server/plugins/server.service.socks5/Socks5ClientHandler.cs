using common.server;
using common.socks5;

namespace server.service.socks5
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class Socks5ClientHandler : ISocks5ClientHandler
    {
        /// <summary>
        /// 
        /// </summary>
        public Socks5ClientHandler()
        {
        }
        /// <summary>
        /// 
        /// </summary>
        public void Flush()
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void InputData(IConnection connection)
        {
            throw new System.NotImplementedException();
        }
    }
}
