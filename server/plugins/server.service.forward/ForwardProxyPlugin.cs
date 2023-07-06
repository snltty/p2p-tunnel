using common.forward;
using common.proxy;
using server.messengers.singnin;
using System;
using System.Collections.Generic;
using System.Linq;

namespace server.service.forward
{
    public sealed class ForwardProxyPlugin : common.forward.ForwardProxyPlugin, IForwardProxyPlugin
    {
        public override HttpHeaderCacheInfo Headers { get; set; }
        public override Memory<byte> HeadersBytes { get; set; }

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

        public override bool HandleRequestData(ProxyInfo info)
        {
            info.ProxyPlugin.Headers = new HttpHeaderCacheInfo { Addr = info.ClientEP.Address, Name = "/", Proxy = Name };
            return base.HandleRequestData(info);
        }
    }
}
