using client.service.ui.api.clientServer;
using System.Threading.Tasks;

namespace client.service.udpforward
{
    /// <summary>
    /// udp转发配置文件
    /// </summary>
    public sealed class UdpForwardClientConfigure : IClientConfigure
    {
        private common.udpforward.Config config;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        public UdpForwardClientConfigure(common.udpforward.Config config)
        {
            this.config = config;
        }

        /// <summary>
        /// 名字
        /// </summary>
        public string Name => "Udp转发";
        /// <summary>
        /// 作者
        /// </summary>
        public string Author => "snltty";
        /// <summary>
        /// 描述
        /// </summary>
        public string Desc => "白名单不为空时只允许白名单内端口";
        /// <summary>
        /// 是否启用
        /// </summary>
        public bool Enable => config.ConnectEnable;

        /// <summary>
        /// 加载配置文件
        /// </summary>
        /// <returns></returns>
        public async Task<string> Load()
        {
            return await config.ReadString();
        }
        /// <summary>
        /// 保存配置文件
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
