using System.Threading.Tasks;

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
        public Task<bool> Request(Socks5Info data);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public Task Response(Socks5Info data);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        public Task ResponseClose(Socks5Info data);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public Task RequestClose(Socks5Info data);
    }

}
