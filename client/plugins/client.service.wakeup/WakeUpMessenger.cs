using common.libs.extends;
using common.server;
using System;

namespace client.service.wakeup
{
    [MessengerIdRange((int)WakeUpMessengerIds.Min, (int)WakeUpMessengerIds.Max)]
    public class WakeUpMessenger : IMessenger
    {
        private readonly WakeUpTransfer wakeUpTransfer;
        private readonly Config config;

        public WakeUpMessenger(WakeUpTransfer wakeUpTransfer, Config config)
        {
            this.wakeUpTransfer = wakeUpTransfer;
            this.config = config;
        }

        [MessengerId((int)WakeUpMessengerIds.Macs)]
        public byte[] Macs(IConnection connection)
        {
            if (connection.ReceiveRequestWrap.Payload.Length > 0)
            {
                wakeUpTransfer.OnNotify(connection);
            }
            return config.ToBytes();
        }

        [MessengerId((int)WakeUpMessengerIds.WakeUp)]
        public void WakeUp(IConnection connection)
        {
            wakeUpTransfer.WakeUp(connection.ReceiveRequestWrap.Payload.GetString());
        }
    }

    [Flags, MessengerIdEnum]
    public enum WakeUpMessengerIds : int
    {
        Min = 1001,
        Macs = 1002,
        WakeUp = 1003,
        Max = 1100,
    }
}
