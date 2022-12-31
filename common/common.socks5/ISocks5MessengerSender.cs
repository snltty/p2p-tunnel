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
        /// <returns></returns>
        public bool Request(Socks5Info data);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public void Response(Socks5Info data);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        public void ResponseClose(Socks5Info data);
      /// <summary>
      /// 
      /// </summary>
      /// <param name="data"></param>
        public void RequestClose(Socks5Info data);
    }

}
