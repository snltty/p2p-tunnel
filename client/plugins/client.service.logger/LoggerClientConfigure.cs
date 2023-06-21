using client.service.ui.api.clientServer;
using System.Threading.Tasks;

namespace client.service.logger
{
    /// <summary>
    /// 日志配置文件
    /// </summary>
    public sealed class LoggerClientConfigure : IClientConfigure
    {
        private readonly Config config;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        public LoggerClientConfigure(Config config)
        {
            this.config = config;
        }

        /// <summary>
        /// 名字
        /// </summary>
        public string Name => "日志信息";
        /// <summary>
        /// 作者
        /// </summary>
        public string Author => "snltty";
        /// <summary>
        /// 描述
        /// </summary>
        public string Desc => "收集日志输出到前端";
        /// <summary>
        /// 启用
        /// </summary>
        public bool Enable => config.Enable;
        /// <summary>
        /// 读取
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
            return true;
        }
    }

}
