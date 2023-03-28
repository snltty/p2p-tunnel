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
        public void Init(int numConnections, int receiveBufferSize);
        public void Start(ushort port, TcpForwardAliveTypes aliveType);
        public Task Response(TcpForwardInfo model);
        public void Stop(ushort sourcePort);
        public void Stop();

        public Func<TcpForwardInfo, Task<bool>> OnRequest { get; set; }
        public Action<ListeningChangeInfo> OnListeningChange { get; set; }
    }

}
