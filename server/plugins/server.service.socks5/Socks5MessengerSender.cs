using common.libs;
using common.server;
using common.server.model;
using common.socks5;
using System;
using System.Threading.Tasks;

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
        public async Task<bool> Request(Socks5Info data)
        {
            return await Task.FromResult(true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public async Task Response(Socks5Info data)
        {
            byte[] bytes = data.ToBytes(out int length);
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                MessengerId = (ushort)Socks5MessengerIds.Response,
                Connection = (data.Tag as IConnection).FromConnection,
                Payload = bytes.AsMemory(0, length)
            });
            data.Return(bytes);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="connection"></param>
        public async Task ResponseClose(Socks5Info data)
        {
            data.Data = Helper.EmptyArray;
            await Response(data);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public async Task RequestClose(Socks5Info data)
        {
            await Task.CompletedTask;
        }

    }
}
