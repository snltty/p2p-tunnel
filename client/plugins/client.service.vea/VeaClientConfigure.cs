using client.service.ui.api.clientServer;
using System.Threading.Tasks;

namespace client.service.vea
{
    public sealed class VeaClientConfigure : IClientConfigure
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

        public async Task<string> Load()
        {
            return await config.ReadString();
        }

        public async Task<string> Save(string jsonStr)
        {
            await config.SaveConfig(jsonStr).ConfigureAwait(false);

            veaSocks5ServerHandler.UpdateConfig();

            return string.Empty;
        }
    }
}
