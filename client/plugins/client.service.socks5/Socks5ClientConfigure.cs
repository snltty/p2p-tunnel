using client.service.ui.api.clientServer;
using common.libs.extends;
using System.Threading.Tasks;

namespace client.service.socks5
{
    public class Socks5ClientConfigure : IClientConfigure
    {
        private common.socks5.Config config;
        public Socks5ClientConfigure(common.socks5.Config config)
        {
            this.config = config;
        }

        public string Name => "socks5";

        public string Author => "snltty";

        public string Desc => "socks5代理";

        public bool Enable => config.ConnectEnable;

        public async Task<string> Load()
        {
            return await config.ReadString();
        }

        public async Task<string> Save(string jsonStr)
        {
            await config.SaveConfig(jsonStr).ConfigureAwait(false);
            return string.Empty;
        }
    }
}
