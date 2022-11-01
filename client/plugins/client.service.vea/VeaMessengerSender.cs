using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using System;
using System.Threading.Tasks;

namespace client.service.vea
{
    public class VeaMessengerSender
    {
        private readonly MessengerSender messengerSender;
        private readonly Config config;
        public VeaMessengerSender(MessengerSender messengerSender, Config config)
        {
            this.messengerSender = messengerSender;
            this.config = config;
        }
        /// <summary>
        /// 获取ip
        /// </summary>
        /// <param name="arg"></param>
        public async Task<IPAddressInfo> IP(IConnection connection)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = connection,
                MessengerId = (int)VeaSocks5MessengerIds.Ip,
                Payload = new IPAddressInfo { IP = config.IP, LanIP = config.LanIP }.ToBytes(),
                Timeout = 1000
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK)
            {
                if (resp.Data.Length > 0)
                {
                    IPAddressInfo ips = new IPAddressInfo();
                    ips.DeBytes(resp.Data);
                    return ips;
                }
            }
            return null;
        }

        public async Task<bool> Reset(IConnection connection, ulong id)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = connection,
                MessengerId = (int)VeaSocks5MessengerIds.Reset,
                Payload = id.ToBytes(),
                Timeout = 15000
            }).ConfigureAwait(false);

            if (resp.Code == MessageResponeCodes.OK)
            {
                return resp.Data.Span.SequenceEqual(Helper.TrueArray);
            }
            return false;

        }
    }
}
