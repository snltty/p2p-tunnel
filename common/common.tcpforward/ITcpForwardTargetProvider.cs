using System.Collections.Generic;

namespace common.tcpforward
{
    /// <summary>
    /// 目标提供适配器，根据 host:port 或者 port查询目标对象，应该发送给谁
    /// </summary>
    public interface ITcpForwardTargetProvider
    {
        /// <summary>
        /// web
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        void Get(string domain, TcpForwardInfo info);
        /// <summary>
        /// tunnel
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        void Get(ushort port, TcpForwardInfo info);
    }
    /// <summary>
    /// 目标缓存器，缓存注册的监听和转发信息，以提供后续查询
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ITcpForwardTargetCaching<T>
    {
        T Get(string host);
        T Get(string domain, ushort port);
        T Get(ushort port);
        bool Add(string domain, ushort port, T mdoel);
        bool Add(ushort port, T mdoel);
        void AddOrUpdate(string domain, ushort port, T mdoel);
        void AddOrUpdate(ushort port, T mdoel);
        bool Remove(string domain, ushort port);
        bool Remove(ushort port);
        IEnumerable<ushort> Remove(string targetName);
        bool Contains(string domain, ushort port);
        bool Contains(ushort port);

        void ClearConnection();
        void ClearConnection(string name);
    }
}