namespace common.server
{
    /// <summary>
    /// 选择中继来源连接对象，用于提供服务端客户端不同实现
    /// </summary>
    public interface IRelaySourceConnectionSelector
    {
        /// <summary>
        /// 选择
        /// </summary>
        /// <param name="connection">接收消息的连接对象</param>
        /// <param name="clientid">客户端id</param>
        /// <returns></returns>
        public IConnection Select(IConnection connection, ulong clientid);
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class RelaySourceConnectionSelector : IRelaySourceConnectionSelector
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="clientid"></param>
        /// <returns></returns>
        public IConnection Select(IConnection connection, ulong clientid) { return connection; }
    }
}
