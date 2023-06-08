using common.libs;
using common.libs.extends;
using common.server;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using System;

namespace common.proxy
{
    [MessengerIdRange((ushort)ProxyMessengerIds.Min, (ushort)ProxyMessengerIds.Max)]
    public sealed class ProxyMessenger : IMessenger
    {
        private readonly IProxyClient proxyClient;
        private readonly IProxyServer proxyServer;
        private readonly IServiceAccessValidator serviceAccessValidator;
        private readonly Config config;
        private readonly ProxyPluginValidatorHandler pluginValidatorHandler;

        public ProxyMessenger(IProxyClient proxyClient, IProxyServer proxyServer, IServiceAccessValidator serviceAccessValidator, Config config, ProxyPluginValidatorHandler pluginValidatorHandler)
        {
            this.proxyClient = proxyClient;
            this.proxyServer = proxyServer;
            this.serviceAccessValidator = serviceAccessValidator;
            this.config = config;
            this.pluginValidatorHandler = pluginValidatorHandler;
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

        [MessengerId((ushort)ProxyMessengerIds.Test)]
        public void Test(IConnection connection)
        {
            if (connection.FromConnection.SendDenied > 0)
            {
                connection.Write(new byte[] { (byte)ProxyConnectTestResult.Denied });
                return;
            }

            ProxyInfo info = ProxyInfo.Debytes(connection.ReceiveRequestWrap.Payload);
            info.Connection = connection.FromConnection;

            if (ProxyPluginLoader.GetPlugin(info.PluginId, out IProxyPlugin plugin) == false)
            {
                connection.Write(new byte[] { (byte)ProxyConnectTestResult.Plugin });
                return;
            }
            info.ProxyPlugin = plugin;
            if (pluginValidatorHandler.Validate(info) == false)
            {
                connection.Write(new byte[] { (byte)ProxyConnectTestResult.Firewail });
                return;
            }
            IPEndPoint remoteEndpoint = ReadRemoteEndPoint(info);
            Socket socket = new Socket(remoteEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, true);
            socket.SendTimeout = 5000;
            try
            {
                IAsyncResult result = socket.BeginConnect(remoteEndpoint, null, null);
                result.AsyncWaitHandle.WaitOne(1000);
                if (result.IsCompleted == false)
                {
                    connection.Write(new byte[] { (byte)ProxyConnectTestResult.Connect });
                    return;
                }
            }
            catch (Exception)
            {
                connection.Write(new byte[] { (byte)ProxyConnectTestResult.Connect });
                return;
            }
            finally
            {
                socket.SafeClose();
            }
            connection.Write(new byte[] { (byte)ProxyConnectTestResult.Success });
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

        private IPEndPoint ReadRemoteEndPoint(ProxyInfo info)
        {
            IPAddress ip = IPAddress.Any;
            switch (info.AddressType)
            {
                case EnumProxyAddressType.IPV4:
                case EnumProxyAddressType.IPV6:
                    {
                        ip = new IPAddress(info.TargetAddress.Span);
                    }
                    break;
                case EnumProxyAddressType.Domain:
                    {
                        ip = NetworkHelper.GetDomainIp(info.TargetAddress.GetString());
                    }
                    break;
                default:
                    break;
            }
            return new IPEndPoint(ip, info.TargetPort);
        }
    }
}
