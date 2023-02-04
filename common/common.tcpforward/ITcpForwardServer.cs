using common.libs;
using System;
using System.Threading.Tasks;

namespace common.tcpforward
{
    /// <summary>
    /// tcp转发监听服务
    /// </summary>
    public interface ITcpForwardServer
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="numConnections"></param>
        /// <param name="receiveBufferSize"></param>
        public void Init(int numConnections, int receiveBufferSize);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        /// <param name="aliveType"></param>
        public void Start(ushort port, TcpForwardAliveTypes aliveType);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        public Task Response(TcpForwardInfo model);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourcePort"></param>
        public void Stop(ushort sourcePort);
        /// <summary>
        /// 
        /// </summary>
        public void Stop();

        /// <summary>
        /// 
        /// </summary>
        public Func<TcpForwardInfo, Task<bool>> OnRequest { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public SimpleSubPushHandler<ListeningChangeInfo> OnListeningChange { get; }
    }
   
}
