using client.service.ui.api.clientServer;
using common.libs.extends;
using System.Threading.Tasks;

namespace client.service.proxy
{
    /// <summary>
    /// proxy
    /// </summary>
    public sealed class ProxyClientService : IClientService
    {
        private readonly common.proxy.Config config;

        public ProxyClientService(common.proxy.Config config)
        {
            this.config = config;
        }

        public common.proxy.Config Get(ClientServiceParamsInfo arg)
        {
            return config;
        }

        public async Task<bool> Add(ClientServiceParamsInfo arg)
        {
            return await config.AddFirewall(arg.Content.DeJson<common.proxy.FirewallItem>());
        }

        public async Task<bool> Remove(ClientServiceParamsInfo arg)
        {
            return await config.RemoveFirewall(uint.Parse(arg.Content));
        }
    }
}
