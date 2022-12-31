using common.libs.extends;
using common.server;
using common.server.model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace client.service.wakeup
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class WakeUpMessengerSender
    {
        private readonly MessengerSender messengerSender;
        private readonly Config config;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="messengerSender"></param>
        /// <param name="config"></param>
        public WakeUpMessengerSender(MessengerSender messengerSender, Config config)
        {
            this.messengerSender = messengerSender;
            this.config = config;
        }
        /// <summary>
        /// 获取mac
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public async Task<List<ConfigItem>> Mac(IConnection connection)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = connection,
                MessengerId = (ushort)WakeUpMessengerIds.Macs,
                Payload = config.ToBytes(),
                Timeout = 2000
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="mac"></param>
        /// <returns></returns>
        public async Task<bool> WakeUp(IConnection connection, string mac)
        {
            return await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = connection,
                MessengerId = (ushort)WakeUpMessengerIds.WakeUp,
                Payload = mac.ToUTF16Bytes(),
                Timeout = 1000
            }).ConfigureAwait(false);

        }
    }
}
