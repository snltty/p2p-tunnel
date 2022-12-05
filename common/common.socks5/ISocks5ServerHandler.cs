using common.server;
using System;

namespace common.socks5
{
    /// <summary>
    /// 
    /// </summary>
    public interface ISocks5ServerHandler
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        void InputData(IConnection connection);
    }
}
