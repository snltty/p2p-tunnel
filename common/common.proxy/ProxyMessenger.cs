using common.server;
using System;
using System.Threading.Tasks;

namespace common.proxy
{
    [MessengerIdRange((ushort)ProxyMessengerIds.Min, (ushort)ProxyMessengerIds.Max)]
    public sealed class ProxyMessenger : IMessenger
    {
        private readonly IProxyClient proxyClient;
        private readonly IProxyServer proxyServer;
        public ProxyMessenger(IProxyClient proxyClient, IProxyServer proxyServer)
        {
            this.proxyClient = proxyClient;
            this.proxyServer = proxyServer;
        }

        [MessengerId((ushort)ProxyMessengerIds.Request)]
        public async Task Request(IConnection connection)
        {
            ProxyInfo data = ProxyInfo.Debytes(connection.ReceiveRequestWrap.Payload);
            data.Connection = connection.FromConnection;
            await proxyClient.InputData(data);
        }


        [MessengerId((ushort)ProxyMessengerIds.Response)]
        public async Task Response(IConnection connection)
        {
            ProxyInfo info = ProxyInfo.Debytes(connection.ReceiveRequestWrap.Payload);
            await proxyServer.InputData(info);
        }
    }
}
