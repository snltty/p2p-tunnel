using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace common.server.servers.iocp
{
    /// <summary>
    /// 
    /// </summary>
    public class SocketAsyncEventArgsPool
    {
        Stack<SocketAsyncEventArgs> pool;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="capacity"></param>
        public SocketAsyncEventArgsPool(int capacity)
        {
            pool = new Stack<SocketAsyncEventArgs>(capacity);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void Push(SocketAsyncEventArgs item)
        {
            if (item == null) { throw new ArgumentNullException("Items added to a SocketAsyncEventArgsPool cannot be null"); }
            lock (pool)
            {
                pool.Push(item);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public SocketAsyncEventArgs Pop()
        {
            lock (pool)
            {
                return pool.Pop();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public int Count
        {
            get { return pool.Count; }
        }
    }
}
