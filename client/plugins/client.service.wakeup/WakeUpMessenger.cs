using common.libs;
using common.libs.extends;
using common.server;
using System;

namespace client.service.wakeup
{
    [MessengerIdRange((ushort)WakeUpMessengerIds.Min, (ushort)WakeUpMessengerIds.Max)]
    public sealed class WakeUpMessenger : IMessenger
    {
        private readonly WakeUpTransfer wakeUpTransfer;
        private readonly Config config;
        public WakeUpMessenger(WakeUpTransfer wakeUpTransfer, Config config)
        {
            this.wakeUpTransfer = wakeUpTransfer;
            this.config = config;
        }

        [MessengerId((ushort)WakeUpMessengerIds.Macs)]
        public void Macs(IConnection connection)
        {
            if (connection.ReceiveRequestWrap.Payload.Length > 0)
            {
                wakeUpTransfer.OnNotify(connection);
            }
            connection.Write(config.ToBytes());
        }
        [MessengerId((ushort)WakeUpMessengerIds.WakeUp)]
        public void WakeUp(IConnection connection)
        {
            wakeUpTransfer.WakeUp(connection.ReceiveRequestWrap.Payload.GetUTF16String());
        }
    }

    /// <summary>
    /// 远程唤醒相关消息id
    /// </summary>
    [Flags, MessengerIdEnum]
    public enum WakeUpMessengerIds : ushort
    {
        /// <summary>
        /// 
        /// </summary>
        Min = 1000,
        /// <summary>
        /// 
        /// </summary>
        Macs = 1002,
        /// <summary>
        /// 
        /// </summary>
        WakeUp = 1003,
        /// <summary>
        /// 
        /// </summary>
        Max = 1099,
    }
}
