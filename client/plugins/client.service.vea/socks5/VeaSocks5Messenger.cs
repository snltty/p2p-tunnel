using common.libs;
using common.server;
using common.socks5;
using System;
using System.Threading.Tasks;

namespace client.service.vea.socks5
{
    /// <summary>
    /// 组网socks5消息
    /// </summary>
    [MessengerIdRange((ushort)VeaSocks5MessengerIds.Min, (ushort)VeaSocks5MessengerIds.Max)]
    public sealed class VeaSocks5Messenger : IMessenger
    {
        private readonly IVeaSocks5ClientHandler socks5ClientHandler;
        private readonly IVeaSocks5ServerHandler socks5ServerHandler;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="socks5ClientHandler"></param>
        /// <param name="socks5ServerHandler"></param>
        public VeaSocks5Messenger(IVeaSocks5ClientHandler socks5ClientHandler, IVeaSocks5ServerHandler socks5ServerHandler)
        {
            this.socks5ClientHandler = socks5ClientHandler;
            this.socks5ServerHandler = socks5ServerHandler;
        }

        /// <summary>
        /// 请求
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)VeaSocks5MessengerIds.Request)]
        public async Task Request(IConnection connection)
        {
            Socks5Info data = Socks5Info.Debytes(connection.ReceiveRequestWrap.Payload);
            data.ClientId = connection.FromConnection.ConnectId;
            data.Tag = connection;
            await socks5ServerHandler.InputData(data);
        }

        /// <summary>
        /// 回执
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)VeaSocks5MessengerIds.Response)]
        public async Task Response(IConnection connection)
        {
            Socks5Info data = Socks5Info.Debytes(connection.ReceiveRequestWrap.Payload);
            await socks5ClientHandler.InputData(data);
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
