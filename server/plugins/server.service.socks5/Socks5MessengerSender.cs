using common.libs;
using common.server;
using common.server.model;
using common.socks5;
using System;

namespace server.service.socks5
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
        /// <param name="messengerSender"></param>
        public Socks5MessengerSender(MessengerSender messengerSender)
        {
            this.messengerSender = messengerSender;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool Request(Socks5Info data)
        {
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public void Response(Socks5Info data)
        {
            byte[] bytes = data.ToBytes(out int length);
            _ = messengerSender.SendOnly(new MessageRequestWrap
            {
                MessengerId = (ushort)Socks5MessengerIds.Response,
                Connection = (data.Tag as IConnection).FromConnection,
                Payload = bytes.AsMemory(0, length)
            }).Result;
            data.Return(bytes);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="connection"></param>
        public void ResponseClose(Socks5Info data)
        {
            data.Data = Helper.EmptyArray;
            Response(data);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public void RequestClose(Socks5Info data)
        {
        }

    }
}
