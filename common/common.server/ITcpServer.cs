using System.Net.Sockets;

namespace common.server
{
    /// <summary>
    /// 
    /// </summary>
    public interface ITcpServer : IServer
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="bufferSize"></param>
        public void SetBufferSize(int bufferSize = 8 * 1024);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="bufferSize"></param>
        /// <returns></returns>
        public IConnection BindReceive(Socket socket, int bufferSize = 8 * 1024);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="socket"></param>
        /// <returns></returns>
        public IConnection CreateConnection(Socket socket);

    }
}
