using client.service.ui.api.clientServer;
using System.Threading.Tasks;

namespace client.service.vea
{
    /// <summary>
    /// 组网配置文件
    /// </summary>
    public sealed class VeaClientConfigure : IClientConfigure
    {
        private Config config;
        private IVeaSocks5ServerHandler veaSocks5ServerHandler;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        /// <param name="veaSocks5ServerHandler"></param>
        public VeaClientConfigure(Config config, IVeaSocks5ServerHandler veaSocks5ServerHandler)
        {
            this.config = config;
            this.veaSocks5ServerHandler = veaSocks5ServerHandler;
        }

        /// <summary>
        /// 名字
        /// </summary>
        public string Name => "virtual adapter";
        /// <summary>
        /// 作者
        /// </summary>
        public string Author => "snltty";

        /// <summary>
        /// 描述
        /// </summary>
        public string Desc => "虚拟网卡";

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

            veaSocks5ServerHandler.UpdateConfig();

            return string.Empty;
        }
    }
}
