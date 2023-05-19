using common.forward;
using common.proxy;
using common.server;
using server.messengers.singnin;
using System;
using System.Collections.Generic;
using System.Linq;

namespace server.service.forward
{
    public interface IForwardProxyPlugin : IProxyPlugin
    {
    }

    public sealed class ForwardProxyPlugin : common.forward.ForwardProxyPlugin, IForwardProxyPlugin
    {
        public ForwardProxyPlugin(common.forward.Config config, IProxyServer proxyServer,
            IForwardTargetProvider forwardTargetProvider, IClientSignInCaching clientSignInCaching,
            IForwardTargetCaching<ForwardTargetCacheInfo> forwardTargetCaching) : base(config, proxyServer, forwardTargetProvider)
        {
            clientSignInCaching.OnOffline += (client) =>
            {
                List<ushort> keys = forwardTargetCaching.Remove(client.ConnectionId).ToList();
                if (keys.Any())
                {
                    foreach (ushort item in keys)
                    {
                        proxyServer.Stop(item);
                    }
                }
            };
        }

    }
}
