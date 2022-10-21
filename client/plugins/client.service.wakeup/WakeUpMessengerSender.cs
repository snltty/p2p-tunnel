using common.libs.extends;
using common.server;
using common.server.model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace client.service.wakeup
{
    public class WakeUpMessengerSender
    {
        private readonly MessengerSender messengerSender;
        private readonly Config config;
        public WakeUpMessengerSender(MessengerSender messengerSender, Config config)
        {
            this.messengerSender = messengerSender;
            this.config = config;
        }
        /// <summary>
        /// 获取mac
        /// </summary>
        /// <param name="arg"></param>
        public async Task<List<ConfigItem>> Mac(IConnection connection)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = connection,
                Path = "wakeup/macs",
                Payload = config.ToBytes(),
                Timeout = 1000
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK)
            {
                if (resp.Data.Length > 0)
                {
                    return Config.DeBytes(resp.Data);
                }
            }
            return new List<ConfigItem>();
        }
        public async Task<bool> WakeUp(IConnection connection, string mac)
        {
            return await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = connection,
                Path = "wakeup/execute",
                Payload = mac.ToBytes(),
                Timeout = 1000
            }).ConfigureAwait(false);

        }
    }
}
