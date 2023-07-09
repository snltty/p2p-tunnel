using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using common.vea;
using System;
using System.Buffers.Binary;
using System.Net;
using System.Threading.Tasks;

namespace client.service.vea
{
    /// <summary>
    /// 组网消息发送
    /// </summary>
    public sealed class VeaMessengerSender
    {
        private readonly MessengerSender messengerSender;
        private readonly Config config;

        public VeaMessengerSender(MessengerSender messengerSender, Config config)
        {
            this.messengerSender = messengerSender;
            this.config = config;
        }

        /// <summary>
        /// 获取在线设备
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public async Task<bool> GetOnLine(IConnection connection)
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = connection,
                MessengerId = (ushort)VeaSocks5MessengerIds.GetOnLine,
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK)
            {
                return resp.Data.Span.SequenceEqual(Helper.TrueArray);
            }
            return false;
        }

        /// <summary>
        /// 发送在线设备
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="veaLanIPAddressOnLine"></param>
        /// <returns></returns>
        public async Task<bool> OnLine(IConnection connection, VeaLanIPAddressOnLine veaLanIPAddressOnLine)
        {
            return await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = connection,
                MessengerId = (ushort)VeaSocks5MessengerIds.OnLine,
                Payload = veaLanIPAddressOnLine.ToBytes(),
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// 更新ip
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public async Task<IPAddressInfo> UpdateIp(IConnection connection)
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = connection,
                MessengerId = (ushort)VeaSocks5MessengerIds.UpdateIp,
                Payload = new IPAddressInfo { IP = BinaryPrimitives.ReadUInt32BigEndian(config.IP.GetAddressBytes()), LanIPs = config.VeaLanIPs }.ToBytes(),
                Timeout = 2000
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK)
            {
                if (resp.Data.Length > 0)
                {
                    IPAddressInfo ips = new IPAddressInfo();
                    ips.DeBytes(resp.Data);
                    return ips;
                }
            }
            return null;
        }

        /// <summary>
        /// 重装其网卡
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<bool> Reset(IConnection connection, ulong id)
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = connection,
                MessengerId = (ushort)VeaSocks5MessengerIds.Reset,
                Payload = id.ToBytes(),
                Timeout = 2000
            }).ConfigureAwait(false);

            if (resp.Code == MessageResponeCodes.OK)
            {
                return resp.Data.Span.SequenceEqual(Helper.TrueArray);
            }
            return false;
        }

        /// <summary>
        /// 分配个ip
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="ip"></param>
        /// <returns></returns>
        public async Task<uint> AssignIP(IConnection connection, byte ip)
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = connection,
                MessengerId = (ushort)VeaSocks5MessengerIds.AssignIP,
                Timeout = 2000,
                Payload = new byte[] { ip },
            }).ConfigureAwait(false);

            if (resp.Code == MessageResponeCodes.OK)
            {
                return  BinaryPrimitives.ReadUInt32BigEndian(resp.Data.Span);
            }
            return 0;

        }
    }
}
