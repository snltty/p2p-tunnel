using System;

namespace common.server.model
{
    interface CryptoInfo
    {
    }

    /// <summary>
    /// 加密相关信息
    /// </summary>
    [Flags, MessengerIdEnum]
    public enum CryptoMessengerIds: ushort
    {
        /// <summary>
        /// 
        /// </summary>
        Min = 200,
        /// <summary>
        /// 获取key
        /// </summary>
        Key = 201,
        /// <summary>
        /// 设置秘钥
        /// </summary>
        Set = 202,
        /// <summary>
        /// 测试
        /// </summary>
        Test = 203,
        /// <summary>
        /// 清除
        /// </summary>
        Clear = 204,
        /// <summary>
        /// 
        /// </summary>
        Max = 299,
    }
}
