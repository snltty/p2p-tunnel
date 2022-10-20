using common.libs.extends;
using common.server;
using System;

namespace client.service.wakeup
{
    public class WakeUpMessenger : IMessenger
    {
        private readonly WakeUpTransfer wakeUpTransfer;
        private readonly Config config;

        public WakeUpMessenger(WakeUpTransfer wakeUpTransfer, Config config)
        {
            this.wakeUpTransfer = wakeUpTransfer;
            this.config = config;
        }

        public byte[] Macs(IConnection connection)
        {
            if (connection.ReceiveRequestWrap.Memory.Length > 0)
            {
                wakeUpTransfer.OnNotify(connection);
            }
            return config.ToBytes();
        }

        public void Execute(IConnection connection)
        {
            wakeUpTransfer.WakeUp(connection.ReceiveRequestWrap.Memory.GetString());
        }
    }
}
