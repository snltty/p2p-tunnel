using client.service.ui.api.clientServer;
using common.libs.extends;
using System.Threading.Tasks;

namespace client.service.wakeup
{
    public class WakeUpClientConfigure : IClientConfigure
    {
        private Config config;
        private readonly WakeUpTransfer wakeUpTransfer;
        public WakeUpClientConfigure(Config config, WakeUpTransfer wakeUpTransfer)
        {
            this.config = config;
            this.wakeUpTransfer = wakeUpTransfer;
        }

        public string Name => "wakeup";

        public string Author => "snltty";

        public string Desc => "远程唤醒";

        public bool Enable => true;

        public async Task<string> Load()
        {
            return await config.ReadString();
        }

        public async Task<string> Save(string jsonStr)
        {
            await config.SaveConfig(jsonStr).ConfigureAwait(false);

            wakeUpTransfer.UpdateMac();

            return string.Empty;
        }
    }
}
