using client.service.ui.api.clientServer;
using common.libs.extends;
using System.Threading.Tasks;

namespace client.service.udpforward
{
    public class UdpForwardClientConfigure : IClientConfigure
    {
        private common.udpforward.Config config;
        public UdpForwardClientConfigure(common.udpforward.Config config)
        {
            this.config = config;
        }

        public string Name => "Udp转发";

        public string Author => "snltty";

        public string Desc => "白名单不为空时只允许白名单内端口";

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
