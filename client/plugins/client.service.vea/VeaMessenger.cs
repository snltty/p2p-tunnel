using common.libs;
using common.server;

namespace client.service.vea
{
    [MessengerIdRange((ushort)VeaSocks5MessengerIds.Min,(ushort)VeaSocks5MessengerIds.Max)]
    public class VeaMessenger : IMessenger
    {
        private readonly VeaTransfer veaTransfer;
        private readonly Config config;
        public VeaMessenger(VeaTransfer veaTransfer, Config config)
        {
            this.veaTransfer = veaTransfer;
            this.config = config;
        }

        [MessengerId((ushort)VeaSocks5MessengerIds.Ip)]
        public byte[] IP(IConnection connection)
        {
            veaTransfer.OnNotify(connection);
            return new IPAddressInfo { IP = config.IP, LanIP = config.LanIPs }.ToBytes();
        }

        [MessengerId((ushort)VeaSocks5MessengerIds.Reset)]
        public byte[] Reset(IConnection connection)
        {
            veaTransfer.Run();
            return Helper.TrueArray;
        }
    }
}
