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
        public byte[] Macs(IConnection connection)
        {
            if (connection.ReceiveRequestWrap.Payload.Length > 0)
            {
                wakeUpTransfer.OnNotify(connection);
            }
            return config.ToBytes();
        }

        [MessengerId((ushort)WakeUpMessengerIds.WakeUp)]
        public void WakeUp(IConnection connection)
        {
            wakeUpTransfer.WakeUp(connection.ReceiveRequestWrap.Payload.GetString());
        }
    }

    [Flags, MessengerIdEnum]
    public enum WakeUpMessengerIds : ushort
    {
        Min = 1000,
        Macs = 1002,
        WakeUp = 1003,
        Max = 1099,
    }
}
