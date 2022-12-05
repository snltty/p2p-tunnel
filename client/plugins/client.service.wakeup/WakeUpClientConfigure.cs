using client.service.ui.api.clientServer;
using System.Threading.Tasks;

namespace client.service.wakeup
{
    /// <summary>
    /// 远程唤醒前端配置
    /// </summary>
    public sealed class WakeUpClientConfigure : IClientConfigure
    {
        private Config config;
        private readonly WakeUpTransfer wakeUpTransfer;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        /// <param name="wakeUpTransfer"></param>
        public WakeUpClientConfigure(Config config, WakeUpTransfer wakeUpTransfer)
        {
            this.config = config;
            this.wakeUpTransfer = wakeUpTransfer;
        }
        /// <summary>
        /// 
        /// </summary>
        public string Name => "wakeup";
        /// <summary>
        /// 
        /// </summary>
        public string Author => "snltty";
        /// <summary>
        /// 
        /// </summary>
        public string Desc => "远程唤醒";
        /// <summary>
        /// 
        /// </summary>
        public bool Enable => true;
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<string> Load()
        {
            return await config.ReadString();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="jsonStr"></param>
        /// <returns></returns>
        public async Task<string> Save(string jsonStr)
        {
            await config.SaveConfig(jsonStr).ConfigureAwait(false);

            wakeUpTransfer.UpdateMac();

            return string.Empty;
        }
    }
}
