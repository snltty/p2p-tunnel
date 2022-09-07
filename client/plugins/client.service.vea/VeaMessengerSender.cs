using common.libs;
using common.server;
using common.server.model;
using System;
using System.Net;
using System.Threading.Tasks;

namespace client.service.vea
{
    public class VeaMessengerSender
    {
        private readonly MessengerSender messengerSender;
        public VeaMessengerSender(MessengerSender messengerSender)
        {
            this.messengerSender = messengerSender;
        }
        /// <summary>
        /// 获取ip
        /// </summary>
        /// <param name="arg"></param>
        public async Task<IPAddress> IP(IConnection connection)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = connection,
                Path = "vea/ip",
                Memory = Helper.EmptyArray,
                Timeout = 1000
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK)
            {
                if (resp.Data.Length > 0)
                {
                    return new IPAddress(resp.Data.Span);
                }
            }
            return IPAddress.Any;
        }
    }
}
