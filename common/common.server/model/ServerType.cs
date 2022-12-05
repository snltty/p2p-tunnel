namespace common.server.model
{
    /// <summary>
    /// 服务类型
    /// </summary>
    public enum ServerType : byte
    {
        /// <summary>
        /// 
        /// </summary>
        TCP = 1,
        /// <summary>
        /// 
        /// </summary>
        UDP = 2, 
        /// <summary>
        /// 
        /// </summary>
        TCPUDP = TCP | UDP
    }
}
