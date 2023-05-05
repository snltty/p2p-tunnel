using common.proxy;
using System;
using System.Collections.Generic;

namespace common.forward
{
    public interface IForwardTargetProvider
    {
        bool Contains(ushort port);
        void Get(string domain, ProxyInfo info);
        void Get(ushort port, ProxyInfo info);
    }

    /// <summary>
    /// 目标缓存器，缓存注册的监听和转发信息，以提供后续查询
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IForwardTargetCaching<T>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        T Get(string host);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        T Get(string domain, ushort port);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        T Get(ushort port);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="port"></param>
        /// <param name="mdoel"></param>
        /// <returns></returns>
        bool Add(string domain, ushort port, T mdoel);
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
        /// <param name="domain"></param>
        /// <param name="port"></param>
        /// <param name="mdoel"></param>
        void AddOrUpdate(string domain, ushort port, T mdoel);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        /// <param name="mdoel"></param>
        void AddOrUpdate(ushort port, T mdoel);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        bool Remove(string domain, ushort port);
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
        /// <param name="domain"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        bool Contains(string domain, ushort port);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        bool Contains(ushort port);

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