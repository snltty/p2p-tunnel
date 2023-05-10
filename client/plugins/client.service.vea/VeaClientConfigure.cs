using client.service.ui.api.clientServer;
using System.Threading.Tasks;

namespace client.service.vea
{
    /// <summary>
    /// 组网配置文件
    /// </summary>
    public sealed class VeaClientConfigure : IClientConfigure
    {
        private readonly Config config;
        private readonly VeaTransfer VeaTransfer;

        public VeaClientConfigure(Config config, VeaTransfer VeaTransfer)
        {
            this.config = config;
            this.VeaTransfer = VeaTransfer;
        }

        /// <summary>
        /// 名字
        /// </summary>
        public string Name => "虚拟网卡组网";
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
            VeaTransfer.UpdateIp();
            return string.Empty;
        }
    }
}
