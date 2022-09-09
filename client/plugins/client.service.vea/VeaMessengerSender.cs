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
        public async Task<(IPAddress, IPAddress)> IP(IConnection connection)
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
                    IPAddress ip = new IPAddress(resp.Data.Span.Slice(1, resp.Data.Span[0]));
                    IPAddress lanip = new IPAddress(resp.Data.Span.Slice(resp.Data.Span[0] + 1));
                    return (ip, lanip);
                }
            }
            return (IPAddress.Any, IPAddress.Any);
        }
    }
}
