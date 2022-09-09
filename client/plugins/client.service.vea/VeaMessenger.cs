using common.server;
using System;

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
            var ip = config.IP.GetAddressBytes();
            var lanip = config.LanIP.GetAddressBytes();
            var bytes = new byte[1 + ip.Length + lanip.Length];

            bytes[0] = (byte)ip.Length;
            Array.Copy(ip, 0, bytes, 1, bytes.Length);
            Array.Copy(ip, 0, lanip, bytes.Length + 1, lanip.Length);

            return bytes;
        }
    }
}
