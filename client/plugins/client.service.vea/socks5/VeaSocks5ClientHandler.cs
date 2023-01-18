using client.messengers.clients;
using common.libs.extends;
using common.server;
using common.socks5;
using System.Net;

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
        private readonly Config config;
        private readonly IClientInfoCaching clientInfoCaching;
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
        public VeaSocks5ClientHandler(IVeaSocks5MessengerSender socks5MessengerSender, Config config, IClientInfoCaching clientInfoCaching,
            IVeaSocks5ClientListener socks5ClientListener, VeaTransfer veaTransfer, IVeaSocks5DstEndpointProvider veaSocks5DstEndpointProvider,ISocks5AuthValidator socks5AuthValidator)
            : base(socks5MessengerSender, veaSocks5DstEndpointProvider, socks5ClientListener, socks5AuthValidator, new common.socks5.Config
            {
                ConnectEnable = config.ConnectEnable,
                NumConnections = config.NumConnections,
                BufferSize = config.BufferSize,
                TargetName = config.TargetName
            })
        {
            this.config = config;
            this.clientInfoCaching = clientInfoCaching;
            this.veaTransfer = veaTransfer;
        }

        /// <summary>
        /// 命令
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected override bool HandleCommand(Socks5Info data)
        {
            var targetEp = Socks5Parser.GetRemoteAddress(data.Data);
            data.Tag = GetConnection(targetEp);
            return base.HndleForwardUdp(data);
        }

        /// <summary>
        /// udp转发
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected override bool HndleForwardUdp(Socks5Info data)
        {
            IPAddress address = Socks5Parser.GetRemoteAddress(data.Data);
            data.Tag = GetConnection(address);
            return base.HndleForwardUdp(data);
        }

        private IConnection GetConnection(IPAddress target)
        {
            if (veaTransfer.IPList.TryGetValue(target, out IPAddressCacheInfo cache))
            {
                return cache.Client.Connection;
            }

            if (target.IsLan())
            {
                int ip = target.GetAddressBytes().ToInt32();
                if (veaTransfer.LanIPList.TryGetValue(ip & 0xffffff, out cache))
                {
                    return cache.Client.Connection;
                }
                if (veaTransfer.LanIPList.TryGetValue(ip & 0xffff, out cache))
                {
                    return cache.Client.Connection;
                }
                if (veaTransfer.LanIPList.TryGetValue(ip & 0xff, out cache))
                {
                    return cache.Client.Connection;
                }
            }
;
            if (clientInfoCaching.GetByName(config.TargetName, out ClientInfo client))
            {
                return client.Connection;
            }
            return null;
        }

    }
}
