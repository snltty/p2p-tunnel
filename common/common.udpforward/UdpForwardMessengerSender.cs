using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using System.Threading.Tasks;

namespace common.udpforward
{
    /// <summary>
    /// udp转发消息发送
    /// </summary>
    public sealed class UdpForwardMessengerSender
    {
        private readonly MessengerSender messengerSender;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="messengerSender"></param>
        public UdpForwardMessengerSender(MessengerSender messengerSender)
        {
            this.messengerSender = messengerSender;
        }

        /// <summary>
        /// 
        /// </summary>
        public SimpleSubPushHandler<UdpForwardInfo> OnRequestHandler { get; } = new SimpleSubPushHandler<UdpForwardInfo>();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public async Task SendRequest(UdpForwardInfo arg)
        {
            var res = messengerSender.SendOnly(new MessageRequestWrap
            {
                MessengerId = (ushort)UdpForwardMessengerIds.Request,
                Connection = arg.Connection,
                Payload = arg.ToBytes()
            }).ConfigureAwait(false);
            await res;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public void OnRequest(UdpForwardInfo data)
        {
            OnRequestHandler.Push(data);
        }

        /// <summary>
        /// 
        /// </summary>
        public SimpleSubPushHandler<UdpForwardInfo> OnResponseHandler { get; } = new SimpleSubPushHandler<UdpForwardInfo>();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="Connection"></param>
        /// <returns></returns>
        public async Task SendResponse(UdpForwardInfo arg, IConnection Connection)
        {
            var res = messengerSender.SendOnly(new MessageRequestWrap
            {
                MessengerId = (ushort)UdpForwardMessengerIds.Response,
                Connection = Connection,
                Payload = arg.ToBytes()
            }).ConfigureAwait(false);
            await res;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public void OnResponse(UdpForwardInfo data)
        {
            OnResponseHandler.Push(data);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Connection"></param>
        /// <returns></returns>
        public async Task<MessageResponeInfo> GetPorts(IConnection Connection)
        {
            return await messengerSender.SendReply(new MessageRequestWrap
            {
                MessengerId = (ushort)UdpForwardMessengerIds.Ports,
                Connection = Connection,
                Payload = Helper.EmptyArray
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Connection"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public async Task<MessageResponeInfo> SignOut(IConnection Connection, ushort port)
        {
            return await messengerSender.SendReply(new MessageRequestWrap
            {
                MessengerId = (ushort)UdpForwardMessengerIds.SignOut,
                Connection = Connection,
                Payload = port.ToBytes()
            }).ConfigureAwait(false);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Connection"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<MessageResponeInfo> SignIn(IConnection Connection, UdpForwardRegisterParamsInfo param)
        {
            return await messengerSender.SendReply(new MessageRequestWrap
            {
                MessengerId = (ushort)UdpForwardMessengerIds.SignIn,
                Connection = Connection,
                Payload = param.ToBytes(),
            }).ConfigureAwait(false);
        }

    }
}
