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

        public async Task<object> Load()
        {
            return await Task.FromResult(config).ConfigureAwait(false);
        }

        public async Task<string> Save(string jsonStr)
        {
            var _config = jsonStr.DeJson<common.udpforward.Config>();

            config.ConnectEnable = _config.ConnectEnable;
            config.TunnelListenRange = _config.TunnelListenRange;
            config.PortWhiteList = _config.PortWhiteList;
            config.PortBlackList = _config.PortBlackList;

            await config.SaveConfig().ConfigureAwait(false);
            return string.Empty;
        }

        public async Task<bool> SwitchEnable(bool enable)
        {
            config.ConnectEnable = enable;
            await config.SaveConfig().ConfigureAwait(false);
            return true;
        }
    }
}
