using common.server;
using common.socks5;
using System;

namespace client.service.vea
{
    /// <summary>
    /// 组网socks5消息
    /// </summary>
    [MessengerIdRange((ushort)VeaSocks5MessengerIds.Min,(ushort)VeaSocks5MessengerIds.Max)]
    public sealed class VeaSocks5Messenger : Socks5Messenger
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="socks5ClientHandler"></param>
        /// <param name="socks5ServerHandler"></param>
        public VeaSocks5Messenger(IVeaSocks5ClientHandler socks5ClientHandler, IVeaSocks5ServerHandler socks5ServerHandler)
            : base(socks5ClientHandler, socks5ServerHandler)
        {

        }

        /// <summary>
        /// 请求
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)VeaSocks5MessengerIds.Request)]
        public new void Request(IConnection connection)
        {
            base.Request(connection);
        }

        /// <summary>
        /// 回复
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)VeaSocks5MessengerIds.Response)]
        public new void Response(IConnection connection)
        {
            base.Response(connection);
        }
    }

    /// <summary>
    /// 组网消息
    /// </summary>
    [Flags, MessengerIdEnum]
    public enum VeaSocks5MessengerIds : ushort
    {
        /// <summary>
        /// 最小
        /// </summary>
        Min = 900,
        /// <summary>
        /// 请求
        /// </summary>
        Request = 902,
        /// <summary>
        /// 回复
        /// </summary>
        Response = 903,
        /// <summary>
        /// 更新ip
        /// </summary>
        Ip = 904,
        /// <summary>
        /// 重装网卡
        /// </summary>
        Reset = 905,
        /// <summary>
        /// 最大
        /// </summary>
        Max = 999,
    }
}
