using common.forward;
using common.proxy;
using server.messengers;
using server.messengers.singnin;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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
                IEnumerable<ushort> keys = forwardTargetCaching.Remove(client.Name);
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
