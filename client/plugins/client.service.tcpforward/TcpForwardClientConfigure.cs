using client.service.ui.api.clientServer;
using common.libs.extends;
using System.Threading.Tasks;

namespace client.service.tcpforward
{
    public class TcpForwardClientConfigure : IClientConfigure
    {
        private common.tcpforward.Config config;
        public TcpForwardClientConfigure(common.tcpforward.Config config)
        {
            this.config = config;
        }

        public string Name => "TCP转发";

        public string Author => "snltty";

        public string Desc => "白名单不为空时只允许白名单内端口";

        public bool Enable => config.ConnectEnable;

        public async Task<object> Load()
        {
            return await Task.FromResult(config).ConfigureAwait(false);
        }

        public async Task<string> Save(string jsonStr)
        {
            var _config = jsonStr.DeJson<common.tcpforward.Config>();

            config. ConnectEnable = _config.ConnectEnable;
            config.NumConnections = _config.NumConnections;
            config.BufferSize = _config.BufferSize;
            config.WebListens = _config.WebListens;
            config.TunnelListenRange = _config.TunnelListenRange;
            config.PortWhiteList = _config.PortWhiteList;
            config.PortBlackList = _config.PortBlackList;
            config.LanConnectEnable = _config.LanConnectEnable;

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
