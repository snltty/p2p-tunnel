using client.service.ui.api.clientServer;
using common.libs.extends;
using System.Threading.Tasks;

namespace client.service.vea
{
    public class VeaClientConfigure : IClientConfigure
    {
        private Config config;
        private IVeaSocks5ServerHandler veaSocks5ServerHandler;
        public VeaClientConfigure(Config config, IVeaSocks5ServerHandler veaSocks5ServerHandler)
        {
            this.config = config;
            this.veaSocks5ServerHandler = veaSocks5ServerHandler;
        }

        public string Name => "virtual adapter";

        public string Author => "snltty";

        public string Desc => "虚拟网卡";

        public bool Enable => config.ConnectEnable;

        public async Task<object> Load()
        {
            return await Task.FromResult(config).ConfigureAwait(false);
        }

        public async Task<string> Save(string jsonStr)
        {
            var _config = jsonStr.DeJson<Config>();

            config.Enable = _config.Enable;
            config.ProxyAll = _config.ProxyAll;
            config.TargetName = _config.TargetName;
            config.IP = _config.IP;
            config.TunnelType = _config.TunnelType;
            config.SocksPort = _config.SocksPort;
            config.BufferSize = _config.BufferSize;
            config.ConnectEnable = _config.ConnectEnable;
            config.LanConnectEnable = _config.LanConnectEnable;

            veaSocks5ServerHandler.UpdateConfig();

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
