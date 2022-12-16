using common.libs;
using common.server;
using common.server.model;
using System;

namespace common.socks5
{
    /// <summary>
    /// socks5消息发送
    /// </summary>
    public class Socks5MessengerSender : ISocks5MessengerSender
    {
        private readonly MessengerSender messengerSender;
        /// <summary>
        /// 
        /// </summary>
        protected virtual ushort TargetRequest { get; } = (ushort)Socks5MessengerIds.Request;
        /// <summary>
        /// 
        /// </summary>
        protected virtual ushort TargetResponse { get; } = (ushort)Socks5MessengerIds.Response;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="messengerSender"></param>
        public Socks5MessengerSender(MessengerSender messengerSender)
        {
            this.messengerSender = messengerSender;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="connection"></param>
        /// <returns></returns>
        public bool Request(Socks5Info data, IConnection connection)
        {
            byte[] bytes = data.ToBytes(out int length);
            bool res = messengerSender.SendOnly(new MessageRequestWrap
            {
                MessengerId = TargetRequest,
                Connection = connection,
                Payload = bytes.AsMemory(0, length)
            }).Result;
            data.Return(bytes);
            return res;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="connection"></param>
        public void Response(Socks5Info data, IConnection connection)
        {
            byte[] bytes = data.ToBytes(out int length);
            _ = messengerSender.SendOnly(new MessageRequestWrap
            {
                MessengerId = TargetResponse,
                Connection = connection,
                Payload = bytes.AsMemory(0, length)
            }).Result;
            data.Return(bytes);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="connection"></param>
        public void ResponseClose(uint id, IConnection connection)
        {
            Response(new Socks5Info { Id = id, Socks5Step = Socks5EnumStep.Forward }, connection);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="connection"></param>
        public void RequestClose(uint id, IConnection connection)
        {
            Request(new Socks5Info { Id = id, Socks5Step = Socks5EnumStep.Forward }, connection);
        }
    }
}
