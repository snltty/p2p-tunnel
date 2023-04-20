using common.httpProxy;
using common.proxy;
using server.messengers;

namespace server.service.httpProxy
{
    public interface IServerHttpProxyPlugin : IHttpProxyPlugin
    {
    }

    public sealed class ServerHttpProxyPlugin : HttpProxyPlugin, IServerHttpProxyPlugin
    {
        public static uint Access => 0b00000000_00000000_00000000_00100000;


        private readonly IServiceAccessValidator serviceAccessProvider;
        public ServerHttpProxyPlugin(common.httpProxy.Config config, IServiceAccessValidator serviceAccessProvider) : base(config)
        {
            this.serviceAccessProvider = serviceAccessProvider;
        }

        public override bool ValidateAccess(ProxyInfo info)
        {
            return serviceAccessProvider.Validate(info.Connection, Access) || base.ValidateAccess(info);
        }
    }
}
