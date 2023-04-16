using client.messengers.clients;
using common.libs.extends;
using common.server;
using common.socks5;
using System;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace client.service.vea.socks5
{
    /// <summary>
    /// 组网socks5客户端
    /// </summary>
    public interface IVeaSocks5ClientHandler : ISocks5ClientHandler
    {
    }

    /// <summary>
    /// 组网socks5客户端
    /// </summary>
    public sealed class VeaSocks5ClientHandler : Socks5ClientHandler, IVeaSocks5ClientHandler
    {
        private readonly VeaTransfer veaTransfer;

        /// <summary>
        /// 组网socks5客户端
        /// </summary>
        /// <param name="socks5MessengerSender"></param>
        /// <param name="config"></param>
        /// <param name="clientInfoCaching"></param>
        /// <param name="socks5ClientListener"></param>
        /// <param name="veaTransfer"></param>
        /// <param name="veaSocks5DstEndpointProvider"></param>
        public VeaSocks5ClientHandler(IVeaSocks5MessengerSender socks5MessengerSender,
            IVeaSocks5ClientListener socks5ClientListener, VeaTransfer veaTransfer, IVeaSocks5DstEndpointProvider veaSocks5DstEndpointProvider)
            : base(socks5MessengerSender, veaSocks5DstEndpointProvider, socks5ClientListener)
        {
            this.veaTransfer = veaTransfer;
        }

        /// <summary>
        /// 命令
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected override async Task<bool> HandleCommand(Socks5Info data)
        {
            if (Socks5Parser.GetIsIPV4(data.Data) == false) return false;
            if (Socks5Parser.GetIsIPV4AnyAddress(data.Data) == false)
            {
                var targetEp = Socks5Parser.GetRemoteAddress(data.Data);
                data.Tag = GetConnection(targetEp);
            }
            return await base.HandleCommand(data);
        }

        /// <summary>
        /// udp转发
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected override async Task<bool> HndleForwardUdp(Socks5Info data)
        {
            if (Socks5Parser.GetIsIPV4(data.Data) == false) return false;
            if (Socks5Parser.GetIsIPV4AnyAddress(data.Data) == false) return false;
            //广播数据包
            if (Socks5Parser.GetIsBroadcastAddress(data.Data))
            {
                foreach (var item in veaTransfer.IPList.Values)
                {
                    data.Tag = item.Client.Connection;
                    await base.HndleForwardUdp(data);
                }
                return true;
            }

            IPAddress address = Socks5Parser.GetRemoteAddress(data.Data);
            data.Tag = GetConnection(address);
            return await base.HndleForwardUdp(data);
        }

        byte[] ipBytes = new byte[4];
        private IConnection GetConnection(IPAddress target)
        {
            IConnection connection = null;
            if (veaTransfer.IPList.TryGetValue(target, out IPAddressCacheInfo cache))
            {
                connection = cache.Client.Connection;
            }
            else
            {
                if (target.IsLan())
                {
                    target.TryWriteBytes(ipBytes, out int len);
                    int ip = ipBytes.AsSpan().ToInt32();
                    if (veaTransfer.LanIPList.TryGetValue(ip & 0xffffff, out cache))
                    {
                        connection = cache.Client.Connection;
                    }
                    if (veaTransfer.LanIPList.TryGetValue(ip & 0xffff, out cache))
                    {
                        connection = cache.Client.Connection;
                    }
                    if (veaTransfer.LanIPList.TryGetValue(ip & 0xff, out cache))
                    {
                        connection = cache.Client.Connection;
                    }
                }
            }

            return connection;
        }

    }
}
