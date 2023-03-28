using client.service.vea.socks5;
using common.libs;
using common.server;

namespace client.service.vea
{
    /// <summary>
    /// 组网消息
    /// </summary>
    [MessengerIdRange((ushort)VeaSocks5MessengerIds.Min, (ushort)VeaSocks5MessengerIds.Max)]
    public sealed class VeaMessenger : IMessenger
    {
        private readonly VeaTransfer veaTransfer;
        private readonly Config config;
        public VeaMessenger(VeaTransfer veaTransfer, Config config)
        {
            this.veaTransfer = veaTransfer;
            this.config = config;
        }

        /// <summary>
        /// 更新ip列表、
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)VeaSocks5MessengerIds.Ip)]
        public void IP(IConnection connection)
        {
            veaTransfer.OnNotify(connection);

            connection.Write(new IPAddressInfo { IP = config.IP, LanIPs = config.LanIPs }.ToBytes());
        }

        /// <summary>
        /// 重装网卡
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)VeaSocks5MessengerIds.Reset)]
        public void Reset(IConnection connection)
        {
            veaTransfer.Run();

            connection.Write(Helper.TrueArray);
        }
    }
}
