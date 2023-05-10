using common.forward;
using common.proxy;
using common.server;
using server.messengers.singnin;
using System.Collections.Generic;
using System.Linq;

namespace server.service.forward
{
    public interface IForwardProxyPlugin : IProxyPlugin
    {
    }

    public sealed class ForwardProxyPlugin : common.forward.ForwardProxyPlugin, IForwardProxyPlugin
    {
        private readonly IServiceAccessValidator serviceAccessProvider;
        private readonly common.forward.Config config;

        public ForwardProxyPlugin(common.forward.Config config, IProxyServer proxyServer, IForwardTargetProvider forwardTargetProvider, IServiceAccessValidator serviceAccessProvider, IClientSignInCaching clientSignInCaching, IForwardTargetCaching<ForwardTargetCacheInfo> forwardTargetCaching) : base(config, proxyServer, forwardTargetProvider, serviceAccessProvider)
        {
            this.config = config;
            this.serviceAccessProvider = serviceAccessProvider;
            clientSignInCaching.OnOffline += (client) =>
            {
                IEnumerable<ushort> keys = forwardTargetCaching.Remove(client.ConnectionId);
                if (keys.Any())
                {
                    foreach (ushort item in keys)
                    {
                        proxyServer.Stop(item);
                    }
                }
            };
        }

        public override bool ValidateAccess(ProxyInfo info)
        {

#if DEBUG
            return true;
#else
            return serviceAccessProvider.Validate(info.Connection, (uint)EnumServiceAccess.Setting);
#endif

        }

    }
}
