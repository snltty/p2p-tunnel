using client.service.ui.api.clientServer;
using System.Threading.Tasks;

namespace client.service.socks5
{
    /// <summary>
    /// socks5配置文件
    /// </summary>
    public sealed class Socks5ClientConfigure : IClientConfigure
    {
        private common.socks5.Config config;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        public Socks5ClientConfigure(common.socks5.Config config)
        {
            this.config = config;
        }

        /// <summary>
        /// 名字
        /// </summary>
        public string Name => "socks5代理";

        /// <summary>
        /// 作者
        /// </summary>
        public string Author => "snltty";
        /// <summary>
        /// 描述
        /// </summary>
        public string Desc => "socks5代理";
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
        public async Task<string> Save(string jsonStr)
        {
            await config.SaveConfig(jsonStr).ConfigureAwait(false);
            return string.Empty;
        }
    }
}
