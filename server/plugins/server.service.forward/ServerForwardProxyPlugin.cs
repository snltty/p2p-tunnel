using common.forward;
using common.libs;
using common.libs.extends;
using common.proxy;
using common.server.model;
using server.messengers;
using server.messengers.singnin;
using System.Linq;
using System.Text;

namespace server.service.forward
{
    public interface IServerForwardProxyPlugin : IProxyPlugin
    {
    }

    public sealed class ServerForwardProxyPlugin : IServerForwardProxyPlugin
    {
        public static uint Access => 0b00000000_00000000_00000000_00010000;
        public byte Id => config.Plugin;
        public EnumBufferSize BufferSize => config.BufferSize;


        private readonly common.forward.Config config;
        private readonly IProxyServer proxyServer;
        private readonly IForwardTargetProvider forwardTargetProvider;
        private readonly IForwardUdpTargetProvider forwardUdpTargetProvider;
        private readonly IServiceAccessValidator serviceAccessProvider;

        public ServerForwardProxyPlugin(common.forward.Config config, IProxyServer proxyServer, IForwardTargetProvider forwardTargetProvider, IForwardUdpTargetProvider forwardUdpTargetProvider, IServiceAccessValidator serviceAccessProvider, IClientSignInCaching clientSignInCaching, IForwardTargetCaching<ForwardTargetCacheInfo> forwardTargetCaching)
        {
            this.config = config;
            this.proxyServer = proxyServer;
            this.forwardTargetProvider = forwardTargetProvider;
            this.forwardUdpTargetProvider = forwardUdpTargetProvider;
            this.serviceAccessProvider = serviceAccessProvider;


            clientSignInCaching.OnOffline += (client) =>
            {
                var keys = forwardTargetCaching.Remove(client.Name);
                if (keys.Any())
                {
                    foreach (var item in keys)
                    {
                        proxyServer.Stop(item);
                    }
                }
            };

            Logger.Instance.Info("代理转发服务已启动...");
            foreach (ushort port in config.WebListens)
            {
                proxyServer.Start(port, config.Plugin);
                Logger.Instance.Warning($"转发监听:{port}");
            }
        }

        public EnumProxyValidateDataResult ValidateData(ProxyInfo info)
        {
            return EnumProxyValidateDataResult.Equal;
        }

        public bool HandleRequestData(ProxyInfo info)
        {
            if (info.Connection == null || info.Connection.Connected == false)
            {
                info.Connection = null;
                GetConnection(info);
            }

            if (info.Connection == null || info.Connection.Connected == false)
            {
                info.Data = Helper.EmptyArray;
                proxyServer.InputData(info);
                return true;
            }

            info.AddressType = EnumProxyAddressType.IPV4;

            return true;
        }
        public void HandleAnswerData(ProxyInfo info) { }
        public bool ValidateAccess(ProxyInfo info)
        {
            return config.ConnectEnable || serviceAccessProvider.Validate(info.Connection, Access);
        }

        private void GetConnection(ProxyInfo info)
        {
            if (info.Command == EnumProxyCommand.UdpAssociate || forwardUdpTargetProvider.Contains(info.ListenPort))
            {
                forwardUdpTargetProvider?.Get(info.ListenPort, info);
            }
            else
            {
                string domain = HttpParser.GetHost(info.Data).GetString();
                forwardTargetProvider?.Get(domain, info);
            }
        }
    }
}
