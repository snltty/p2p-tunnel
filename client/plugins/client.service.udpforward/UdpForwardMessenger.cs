using common.server;
using common.udpforward;

namespace client.service.udpforward
{
    /// <summary>
    /// udp转发消息器
    /// </summary>
    [MessengerIdRange((ushort)UdpForwardMessengerIds.Min, (ushort)UdpForwardMessengerIds.Max)]
    public sealed class UdpForwardMessenger : IMessenger
    {
        private readonly UdpForwardMessengerSender udpForwardMessengerSender;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="udpForwardMessengerSender"></param>
        public UdpForwardMessenger(UdpForwardMessengerSender udpForwardMessengerSender)
        {
            this.udpForwardMessengerSender = udpForwardMessengerSender;
        }

        /// <summary>
        /// 请求消息
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)UdpForwardMessengerIds.Request)]
        public void Request(IConnection connection)
        {
            UdpForwardInfo data = new UdpForwardInfo();
            data.Connection = connection;
            data.DeBytes(connection.ReceiveRequestWrap.Payload);
            udpForwardMessengerSender.OnRequest(data);
        }

        /// <summary>
        /// 回复消息
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)UdpForwardMessengerIds.Response)]
        public void Response(IConnection connection)
        {
            UdpForwardInfo data = new UdpForwardInfo();
            data.Connection = connection;
            data.DeBytes(connection.ReceiveRequestWrap.Payload);
            udpForwardMessengerSender.OnResponse(data);
        }
    }
}
