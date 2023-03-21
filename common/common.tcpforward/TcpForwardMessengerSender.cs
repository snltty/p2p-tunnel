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
        public TcpForwardMessengerSender(MessengerSender messengerSender)
        {
            this.messengerSender = messengerSender;
        }
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
        public async Task<MessageResponeInfo> GetPorts(IConnection Connection)
        {
            return await messengerSender.SendReply(new MessageRequestWrap
            {
                MessengerId = (ushort)TcpForwardMessengerIds.Ports,
                Connection = Connection
            }).ConfigureAwait(false);
        }
        public async Task<MessageResponeInfo> UnRegister(IConnection Connection, TcpForwardUnRegisterParamsInfo data)
        {
            return await messengerSender.SendReply(new MessageRequestWrap
            {
                MessengerId = (ushort)TcpForwardMessengerIds.SignOut,
                Connection = Connection,
                Payload = data.ToBytes()
            }).ConfigureAwait(false);
        }
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
