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

    public enum EnumBufferSize : byte
    {
        KB_1 = 0,
        KB_2 = 1,
        KB_4 = 2,
        KB_8 = 3,
        KB_16 = 4,
        KB_32 = 5,
        KB_64 = 6,
        KB_128 = 7,
        KB_256 = 8,
        KB_512 = 9,
        KB_1024 = 10,
    }
}
