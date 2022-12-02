using common.libs;
using common.server;
using common.server.model;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace client.realize.messengers.heart
{
    public sealed class HeartMessengerSender
    {
        private readonly MessengerSender messengerSender;
        public HeartMessengerSender(MessengerSender messengerSender)
        {
            this.messengerSender = messengerSender;
        }
        /// <summary>
        /// 发送心跳消息
        /// </summary>
        /// <param name="arg"></param>
        public async Task<bool> Heart(IConnection connection)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = connection,
                MessengerId = (ushort)HeartMessengerIds.Alive,
                Payload = Helper.EmptyArray,
                Timeout = 2000
            }).ConfigureAwait(false);
            return resp.Code == MessageResponeCodes.OK && Helper.TrueArray.AsSpan().SequenceEqual(resp.Data.Span);
        }
    }
}
