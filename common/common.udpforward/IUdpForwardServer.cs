using common.libs;

namespace common.udpforward
{
    /// <summary>
    /// tcp转发监听服务
    /// </summary>
    public interface IUdpForwardServer
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourcePort"></param>
        public void Start(ushort sourcePort);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        public void Response(UdpForwardInfo model);
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
        public SimpleSubPushHandler<UdpForwardInfo> OnRequest { get; }
        /// <summary>
        /// 
        /// </summary>
        public SimpleSubPushHandler<UdpforwardListenChangeInfo> OnListenChange { get; }
    }
   
    /// <summary>
    /// 
    /// </summary>
    public sealed class UdpforwardListenChangeInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public int Port { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool State { get; set; }

    }
}
