using client.messengers.singnin;
using common.forward;
using common.proxy;
using System;

namespace client.service.forward
{
    public class ClientForwardProxyPlugin : ForwardProxyPlugin, IForwardProxyPlugin
    {
        public override HttpHeaderCacheInfo Headers { get; set; }
        public override Memory<byte> HeadersBytes { get; set; }

        public ClientForwardProxyPlugin(common.forward.Config config, Config config1, IProxyServer proxyServer, IForwardTargetProvider forwardTargetProvider
            , SignInStateInfo signInStateInfo) : base(config, proxyServer, forwardTargetProvider)
        {
            signInStateInfo.OnChange += (bool state) =>
            {
                Headers = new HttpHeaderCacheInfo
                {
                    Addr =  signInStateInfo.RemoteInfo.Ip,
                    Name = config1.Client.Name,
                    Proxy = Name
                };
            };
        }

    }
}
