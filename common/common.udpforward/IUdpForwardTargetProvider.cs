using System.Collections.Generic;

namespace common.udpforward
{
    /// <summary>
    /// 
    /// </summary>
    public interface IUdpForwardTargetProvider
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourcePort"></param>
        /// <param name="info"></param>
        void Get(ushort sourcePort, UdpForwardInfo info);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IUdpForwardTargetCaching<T>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        T Get(ushort port);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        /// <param name="mdoel"></param>
        /// <returns></returns>
        bool Add(ushort port, T mdoel);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        bool Remove(ushort port);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetName"></param>
        /// <returns></returns>
        IEnumerable<ushort> Remove(string targetName);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        IEnumerable<ushort> Remove(ulong id);
        /// <summary>
        /// 
        /// </summary>
        void ClearConnection();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        void ClearConnection(string name);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        void ClearConnection(ulong id);
    }
}