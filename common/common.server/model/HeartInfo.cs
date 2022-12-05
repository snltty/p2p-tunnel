using System;

namespace common.server.model
{
    /// <summary>
    /// 心跳相关消息id
    /// </summary>
    [Flags, MessengerIdEnum]
    public enum HeartMessengerIds : ushort
    {
        /// <summary>
        /// 
        /// </summary>
        Min = 300,
        /// <summary>
        /// 活着
        /// </summary>
        Alive = 301,
        /// <summary>
        /// 
        /// </summary>
        Max = 399,
    }
}
