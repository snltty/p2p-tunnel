using System.Net;
using System.Threading.Tasks;

namespace common.server
{
    /// <summary>
    /// 
    /// </summary>
    public interface IUdpServer : IServer
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        /// <param name="timeout"></param>
        public void Start(int port, int timeout = 20000);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public Task<IConnection> CreateConnection(IPEndPoint address);

    }
}
