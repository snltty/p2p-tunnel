using client.messengers.clients;
using common.libs.extends;
using common.server;
using common.socks5;
using System.Net;
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
        private readonly Config config;
        private readonly IClientInfoCaching clientInfoCaching;
        private readonly VeaTransfer veaTransfer;
        private readonly IClientsTransfer clientsTransfer;

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
            IVeaSocks5ClientListener socks5ClientListener, VeaTransfer veaTransfer, IVeaSocks5DstEndpointProvider veaSocks5DstEndpointProvider, IClientsTransfer clientsTransfer)
            : base(socks5MessengerSender, veaSocks5DstEndpointProvider, socks5ClientListener)
        {
            this.config = config;
            this.clientInfoCaching = clientInfoCaching;
            this.veaTransfer = veaTransfer;
            this.clientsTransfer = clientsTransfer;
        }

        /// <summary>
        /// 命令
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected override async Task<bool> HandleCommand(Socks5Info data)
        {
            var targetEp = Socks5Parser.GetRemoteAddress(data.Data);
            data.Tag = GetConnection(targetEp);
            return await base.HndleForwardUdp(data);
        }

        /// <summary>
        /// udp转发
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected override async Task<bool> HndleForwardUdp(Socks5Info data)
        {
            IPAddress address = Socks5Parser.GetRemoteAddress(data.Data);
            data.Tag = GetConnection(address);
            return await base.HndleForwardUdp(data);
        }

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
                    int ip = target.GetAddressBytes().ToInt32();
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
;
                if (clientInfoCaching.GetByName(config.TargetName, out ClientInfo client))
                {
                    connection = client.Connection;
                    if (connection == null || connection.Connected == false)
                    {
                        clientsTransfer.ConnectClient(client);
                    }
                }
            }

            return connection;
        }

    }
}
