using common.libs;
using common.server;
using System;
using System.Buffers.Binary;

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
            veaTransfer.ResetMask(config.VeaLanIPs);
            connection.Write(new IPAddressInfo { IP = BinaryPrimitives.ReadUInt32BigEndian(config.IP.GetAddressBytes()), LanIPs = config.VeaLanIPs }.ToBytes());
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


    /// <summary>
    /// 组网消息
    /// </summary>
    [Flags, MessengerIdEnum]
    public enum VeaSocks5MessengerIds : ushort
    {
        /// <summary>
        /// 最小
        /// </summary>
        Min = 1100,
        /// <summary>
        /// 更新ip
        /// </summary>
        Ip = 1101,
        /// <summary>
        /// 重装网卡
        /// </summary>
        Reset = 1102,
        /// <summary>
        /// 最大
        /// </summary>
        Max = 1199,
    }
}
