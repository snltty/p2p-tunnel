using common.server;

namespace client.service.vea
{
    public class VeaMessenger : IMessenger
    {
        private readonly Config config;
        public VeaMessenger(Config config)
        {
            this.config = config;
        }

        public byte[] IP(IConnection connection)
        {
            return config.IP.GetAddressBytes();
        }
    }
}
