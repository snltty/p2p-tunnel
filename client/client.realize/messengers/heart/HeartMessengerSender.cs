using common.libs;
using common.server;
using common.server.model;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace client.realize.messengers.heart
{
    /// <summary>
    /// 心跳消息发送
    /// </summary>
    public sealed class HeartMessengerSender
    {
        private readonly MessengerSender messengerSender;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="messengerSender"></param>
        public HeartMessengerSender(MessengerSender messengerSender)
        {
            this.messengerSender = messengerSender;
        }
        /// <summary>
        /// 发送心跳消息
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public async Task<bool> Heart(IConnection connection)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = connection,
                MessengerId = (ushort)HeartMessengerIds.Alive,
                Timeout = 2000
            },false).ConfigureAwait(false);
            return resp.Code == MessageResponeCodes.OK && Helper.TrueArray.AsSpan().SequenceEqual(resp.Data.Span);
        }
    }
}
