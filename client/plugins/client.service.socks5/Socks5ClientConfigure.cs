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

        public async Task<object> Load()
        {
            return await Task.FromResult(config).ConfigureAwait(false);
        }

        public async Task<string> Save(string jsonStr)
        {
            var _config = jsonStr.DeJson< common.socks5.Config> ();

            config.ListenEnable = _config.ListenEnable;
            config.ConnectEnable = _config.ConnectEnable;
            config.ListenPort = _config.ListenPort;
            config.BufferSize = _config.BufferSize;
            config.IsPac = _config.IsPac;
            config.IsCustomPac = _config.IsCustomPac;
            config.TargetName = _config.TargetName;
            config.TunnelType = _config.TunnelType;
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
