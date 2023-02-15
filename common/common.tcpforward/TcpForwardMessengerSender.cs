using common.libs;
using common.server;
using common.server.model;
using System;
using System.Threading.Tasks;

namespace common.tcpforward
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class TcpForwardMessengerSender
    {
        private readonly MessengerSender messengerSender;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="messengerSender"></param>
        public TcpForwardMessengerSender(MessengerSender messengerSender)
        {
            this.messengerSender = messengerSender;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public async Task<bool> SendRequest(TcpForwardInfo arg)
        {
            byte[] bytes = arg.ToBytes(out int length);

            bool res = await messengerSender.SendOnly(new MessageRequestWrap
            {
                MessengerId = (ushort)TcpForwardMessengerIds.Request,
                Connection = arg.Connection,
                Payload = bytes.AsMemory(0, length)
            });
            arg.Return(bytes);

            return res;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="connection"></param>
        public async Task<bool> SendResponse(TcpForwardInfo arg, IConnection connection)
        {
            byte[] bytes = arg.ToBytes(out int length);

            bool res = await messengerSender.SendOnly(new MessageRequestWrap
            {
                MessengerId = (ushort)TcpForwardMessengerIds.Response,
                Connection = connection,
                Payload = bytes.AsMemory(0, length)
            });

            arg.Return(bytes);

            return res;
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
                MessengerId = (ushort)TcpForwardMessengerIds.Ports,
                Connection = Connection
            }).ConfigureAwait(false);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Connection"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<MessageResponeInfo> UnRegister(IConnection Connection, TcpForwardUnRegisterParamsInfo data)
        {
            return await messengerSender.SendReply(new MessageRequestWrap
            {
                MessengerId = (ushort)TcpForwardMessengerIds.SignOut,
                Connection = Connection,
                Payload = data.ToBytes()
            }).ConfigureAwait(false);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Connection"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<MessageResponeInfo> Register(IConnection Connection, TcpForwardRegisterParamsInfo data)
        {
            return await messengerSender.SendReply(new MessageRequestWrap
            {
                MessengerId = (ushort)TcpForwardMessengerIds.SignIn,
                Connection = Connection,
                Payload = data.ToBytes(),
            }).ConfigureAwait(false);
        }

    }
}
