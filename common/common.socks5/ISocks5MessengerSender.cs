using common.server;

namespace common.socks5
{
    /// <summary>
    /// 
    /// </summary>
    public interface ISocks5MessengerSender
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="connection"></param>
        /// <returns></returns>
        public bool Request(Socks5Info data, IConnection connection);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="connection"></param>
        public void Response(Socks5Info data, IConnection connection);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="connection"></param>
        public void ResponseClose(uint id, IConnection connection);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="connection"></param>
        public void RequestClose(uint id, IConnection connection);
    }
}
