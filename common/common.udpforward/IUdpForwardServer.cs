using common.libs;
using System;
using System.Threading.Tasks;

namespace common.udpforward
{
    /// <summary>
    /// tcp转发监听服务
    /// </summary>
    public interface IUdpForwardServer
    {
        public void Start(ushort sourcePort);
        public Task Response(UdpForwardInfo model);
        public void Stop(ushort sourcePort);
        public void Stop();

        public Func<UdpForwardInfo, Task> OnRequest { get; set; }
        public Action<UdpforwardListenChangeInfo> OnListenChange { get; set; }
    }

    public sealed class UdpforwardListenChangeInfo
    {
        public int Port { get; set; }
        public bool State { get; set; }

    }
}
