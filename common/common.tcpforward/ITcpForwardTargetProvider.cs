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
        void Get(int port, TcpForwardInfo info);
    }
    /// <summary>
    /// 目标缓存器，缓存注册的监听和转发信息，以提供后续查询
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ITcpForwardTargetCaching<T>
    {
        T Get(string host);
        T Get(string domain, int port);
        T Get(int port);
        bool Add(string domain, int port, T mdoel);
        bool Add(int port, T mdoel);
        void AddOrUpdate(string domain, int port, T mdoel);
        void AddOrUpdate(int port, T mdoel);
        bool Remove(string domain, int port);
        bool Remove(int port);
        IEnumerable<int> Remove(string targetName);
        bool Contains(string domain, int port);
        bool Contains(int port);

        void ClearConnection();
        void ClearConnection(string name);
    }
}