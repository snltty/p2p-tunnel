using common.forward;
using common.libs;
using common.libs.extends;
using common.proxy;
using common.server.model;
using server.messengers;
using server.messengers.singnin;
using System;
using System.Linq;
using System.Text;

namespace server.service.forward
{
    public interface IForwardProxyPlugin : IProxyPlugin
    {
    }

    public sealed class ForwardProxyPlugin : common.forward.ForwardProxyPlugin, IForwardProxyPlugin
    {
        public static uint Access => 0b00000000_00000000_00000000_00001000;
        private readonly IServiceAccessValidator serviceAccessProvider;

        public ForwardProxyPlugin(common.forward.Config config, IProxyServer proxyServer, IForwardTargetProvider forwardTargetProvider, IServiceAccessValidator serviceAccessProvider, IClientSignInCaching clientSignInCaching, IForwardTargetCaching<ForwardTargetCacheInfo> forwardTargetCaching) : base(config, proxyServer, forwardTargetProvider)
        {
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

            Logger.Instance.Info("端口转发穿透服务已启动...");
            foreach (ushort port in config.WebListens)
            {
                proxyServer.Start(port, config.Plugin);
                Logger.Instance.Warning($"端口转发穿透监听:{port}");
            }
        }

        public override bool ValidateAccess(ProxyInfo info)
        {
            return true;//serviceAccessProvider.Validate(info.Connection, (uint)EnumServiceAccess.Setting);
        }

    }
}
