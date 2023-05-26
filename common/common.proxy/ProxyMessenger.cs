using common.libs;
using common.libs.extends;
using common.server;
using System.Threading.Tasks;

namespace common.proxy
{
    [MessengerIdRange((ushort)ProxyMessengerIds.Min, (ushort)ProxyMessengerIds.Max)]
    public sealed class ProxyMessenger : IMessenger
    {
        private readonly IProxyClient proxyClient;
        private readonly IProxyServer proxyServer;
        private readonly IServiceAccessValidator serviceAccessValidator;
        private readonly Config config;

        public ProxyMessenger(IProxyClient proxyClient, IProxyServer proxyServer, IServiceAccessValidator serviceAccessValidator, Config config)
        {
            this.proxyClient = proxyClient;
            this.proxyServer = proxyServer;
            this.serviceAccessValidator = serviceAccessValidator;
            this.config = config;
        }

        [MessengerId((ushort)ProxyMessengerIds.Request)]
        public async Task Request(IConnection connection)
        {
            if (connection.FromConnection.SendDenied > 0) return;

            ProxyInfo info = ProxyInfo.Debytes(connection.ReceiveRequestWrap.Payload);
            info.Connection = connection.FromConnection;
            info.Connection.SentBytes += (uint)info.Data.Length;
            await proxyClient.InputData(info);
        }


        [MessengerId((ushort)ProxyMessengerIds.Response)]
        public async Task Response(IConnection connection)
        {
            if (connection.FromConnection.SendDenied > 0) return;
            ProxyInfo info = ProxyInfo.Debytes(connection.ReceiveRequestWrap.Payload);
            connection.FromConnection.SentBytes += (ulong)info.Data.Length;
            await proxyServer.InputData(info);
        }


        [MessengerId((ushort)ProxyMessengerIds.GetFirewall)]
        public void GetFirewall(IConnection connection)
        {
            if (serviceAccessValidator.Validate(connection.ConnectId, (uint)EnumServiceAccess.Setting))
            {
                connection.FromConnection.Write(config.ToJson().ToUTF8Bytes());
            }
            else
            {
                connection.FromConnection.Write(new Config().ToJson().ToUTF8Bytes());
            }
        }

        [MessengerId((ushort)ProxyMessengerIds.AddFirewall)]
        public async Task AddFirewall(IConnection connection)
        {
            if (serviceAccessValidator.Validate(connection.ConnectId, (uint)EnumServiceAccess.Setting))
            {
                FirewallItem item = connection.ReceiveRequestWrap.Payload.GetUTF8String().DeJson<FirewallItem>();
                await config.AddFirewall(item);
                connection.FromConnection.Write(Helper.TrueArray);
            }
            else
            {
                connection.FromConnection.Write(Helper.FalseArray);
            }
        }

        [MessengerId((ushort)ProxyMessengerIds.RemoveFirewall)]
        public async Task RemoveFirewall(IConnection connection)
        {
            if (serviceAccessValidator.Validate(connection.ConnectId, (uint)EnumServiceAccess.Setting))
            {
                await config.RemoveFirewall(connection.ReceiveRequestWrap.Payload.ToUInt32());
                connection.FromConnection.Write(Helper.TrueArray);
            }
            else
            {
                connection.FromConnection.Write(Helper.FalseArray);
            }
        }
    }
}
