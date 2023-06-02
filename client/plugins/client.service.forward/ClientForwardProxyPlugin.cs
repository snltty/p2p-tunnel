using client.messengers.singnin;
using common.forward;
using common.proxy;

namespace client.service.forward
{
    public class ClientForwardProxyPlugin : ForwardProxyPlugin, IForwardProxyPlugin
    {
        public override HttpHeaderDynamicInfo Headers { get; set; }

        public ClientForwardProxyPlugin(common.forward.Config config,Config config1, IProxyServer proxyServer, IForwardTargetProvider forwardTargetProvider
            , SignInStateInfo signInStateInfo) : base(config, proxyServer, forwardTargetProvider)
        {
            signInStateInfo.OnChange += (bool state) =>
            {
                Headers = new HttpHeaderDynamicInfo
                {
                    Addr = signInStateInfo.RemoteInfo.Ip,
                    Name = config1.Client.Name
                };
            };
        }

    }
}
