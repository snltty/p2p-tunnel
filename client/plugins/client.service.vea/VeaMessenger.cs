using common.libs;
using common.libs.extends;
using common.server;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

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
        private readonly VeaMessengerSender veaMessengerSender;

        private bool running = false;

        private VeaLanIPAddressOnLine veaLanIPAddressOnLine = new VeaLanIPAddressOnLine();

        public VeaMessenger(VeaTransfer veaTransfer, Config config, VeaMessengerSender veaMessengerSender)
        {
            this.veaTransfer = veaTransfer;
            this.config = config;
            this.veaMessengerSender = veaMessengerSender;
        }

        [MessengerId((ushort)VeaSocks5MessengerIds.GetOnLine)]
        public async Task GetOnLine(IConnection connection)
        {
            if (running == false)
            {
                running = true;
                _ = Task.Run(async () =>
                 {
                     byte[] bytes = new byte[4];
                     foreach (VeaLanIPAddress item in config.VeaLanIPs)
                     {
                         //从网络号，到广播号
                         for (uint i = item.NetWork; i <= item.Broadcast; i++)
                         {
                             BinaryPrimitives.WriteUInt32BigEndian(bytes, i);

                             if (veaLanIPAddressOnLine.Items.TryGetValue(i, out VeaLanIPAddressOnLineItem onlineItem) == false)
                             {
                                 onlineItem = new VeaLanIPAddressOnLineItem();
                                 veaLanIPAddressOnLine.Items[i] = onlineItem;
                             }
                             try
                             {
                                 using Ping ping = new Ping();
                                 PingReply reply = await ping.SendPingAsync(new IPAddress(bytes)).WaitAsync(TimeSpan.FromMilliseconds(30));

                                 onlineItem.Online =  reply.Status == IPStatus.Success;
                                 if (onlineItem.Online == false)
                                 {
                                     veaLanIPAddressOnLine.Items.Remove(i);
                                 }
                             }
                             catch (Exception)
                             {
                                 veaLanIPAddressOnLine.Items.Remove(i);
                             }
                         }
                     }

                     foreach (var item in veaLanIPAddressOnLine.Items.Where(c => c.Value.Online == false))
                     {
                         veaLanIPAddressOnLine.Items.Remove(item.Key);
                     }

                     await veaMessengerSender.OnLine(connection.FromConnection, veaLanIPAddressOnLine);

                     DateTime start = DateTime.Now;
                     foreach (var item in veaLanIPAddressOnLine.Items)
                     {
                         if (item.Value.Online)
                         {
                             BinaryPrimitives.WriteUInt32BigEndian(bytes, item.Key);
                             try
                             {
                                 IPHostEntry hostEntry = Dns.GetHostEntry(new IPAddress(bytes));
                                 if (string.IsNullOrWhiteSpace(hostEntry.HostName) == false)
                                 {
                                     item.Value.Name = hostEntry.HostName;
                                 }
                                 if ((DateTime.Now - start).Seconds > 1)
                                 {
                                     start = DateTime.Now;
                                     await veaMessengerSender.OnLine(connection.FromConnection, veaLanIPAddressOnLine);
                                 }
                             }
                             catch (Exception)
                             {
                             }
                         }

                     }
                     await veaMessengerSender.OnLine(connection.FromConnection, veaLanIPAddressOnLine);

                     running = false;
                 });
            }
            else
            {
                await veaMessengerSender.OnLine(connection.FromConnection, veaLanIPAddressOnLine);
            }
            connection.FromConnection.Write(Helper.TrueArray);
        }

        [MessengerId((ushort)VeaSocks5MessengerIds.OnLine)]
        public void OnLine(IConnection connection)
        {
            VeaLanIPAddressOnLine veaLanIPAddressOnLine = new VeaLanIPAddressOnLine();
            veaLanIPAddressOnLine.DeBytes(connection.ReceiveRequestWrap.Payload);
            veaTransfer.OnOnline(connection.FromConnection.ConnectId, veaLanIPAddressOnLine);
        }

        /// <summary>
        /// 更新ip列表、
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)VeaSocks5MessengerIds.Ip)]
        public void IP(IConnection connection)
        {
            IPAddressInfo ips = new IPAddressInfo();
            ips.DeBytes(connection.ReceiveRequestWrap.Payload);

            Task.Run(() =>
            {
                veaTransfer.OnIPs(connection.FromConnection.ConnectId, ips);
            });

            uint ip = BinaryPrimitives.ReadUInt32BigEndian(config.IP.GetAddressBytes());
            IPAddressInfo iPAddressInfo = new IPAddressInfo { IP = ip, LanIPs = config.VeaLanIPs };
            connection.FromConnection?.Write(iPAddressInfo.ToBytes());
        }

        /// <summary>
        /// 重装网卡
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)VeaSocks5MessengerIds.Reset)]
        public void Reset(IConnection connection)
        {
            Task.Run(() =>
            {
                veaTransfer.Run();
            });
            connection.FromConnection?.Write(Helper.TrueArray);
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
        /// 获取在线设备
        /// </summary>
        GetOnLine = 1103,
        /// <summary>
        /// 在线设备数据
        /// </summary>
        OnLine = 1104,
        /// <summary>
        /// 最大
        /// </summary>
        Max = 1199,
    }
}
