using common.server;

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
        /// <summary>
        /// 
        /// </summary>
        void Flush();
    }
}
