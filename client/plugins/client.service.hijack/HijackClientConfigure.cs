using client.service.ui.api.clientServer;
using System.Threading.Tasks;

namespace client.service.hijack
{
    /// <summary>
    /// 组网配置文件
    /// </summary>
    public sealed class HijackClientConfigure : IClientConfigure
    {
        private readonly Config config;

        public HijackClientConfigure(Config config)
        {
            this.config = config;
        }

        /// <summary>
        /// 名字
        /// </summary>
        public string Name => "劫持代理";
        /// <summary>
        /// 作者
        /// </summary>
        public string Author => "snltty";

        /// <summary>
        /// 描述
        /// </summary>
        public string Desc => "劫持代理";

        /// <summary>
        /// 启用
        /// </summary>
        public bool Enable => config.ConnectEnable;

        /// <summary>
        /// 加载
        /// </summary>
        /// <returns></returns>
        public async Task<string> Load()
        {
            return await config.ReadString();
        }

        /// <summary>
        /// 保存
        /// </summary>
        /// <param name="jsonStr"></param>
        /// <returns></returns>
        public async Task<bool> Save(string jsonStr)
        {
            await config.SaveConfig(jsonStr).ConfigureAwait(false);
            _ = Task.Run(() =>
            {
            });
            return true;
        }
    }
}
