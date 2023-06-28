using common.libs.database;
using common.libs.extends;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;

namespace client.service.hijack
{
    /// <summary>
    /// 劫持代理配置文件
    /// </summary>
    [Table("hijack-appsettings")]
    public sealed class Config
    {
        public Config() { }
        private readonly IConfigDataProvider<Config> configDataProvider;


        public Config(IConfigDataProvider<Config> configDataProvider)
        {
            this.configDataProvider = configDataProvider;

            Config config = ReadConfig().Result;
            ListenEnable = config.ListenEnable;
            ProxyAll = config.ProxyAll;
            ConnectEnable = config.ConnectEnable;
            SaveConfig().Wait();
        }

        /// <summary>
        /// 启用
        /// </summary>
        public bool ListenEnable { get; set; }
        /// <summary>
        /// 代理所有
        /// </summary>
        public bool ProxyAll { get; set; }
        /// <summary>
        /// 允许被连接
        /// </summary>
        public bool ConnectEnable { get; set; }

        /// <summary>
        /// 读取配置文件
        /// </summary>
        /// <returns></returns>
        public async Task<Config> ReadConfig()
        {
            return await configDataProvider.Load() ?? new Config();
        }
        /// <summary>
        /// 读取配置文件
        /// </summary>
        /// <returns></returns>
        public async Task<string> ReadString()
        {
            return await configDataProvider.LoadString();
        }
        /// <summary>
        /// 保存配置文件
        /// </summary>
        /// <param name="jsonStr"></param>
        /// <returns></returns>
        public async Task SaveConfig(string jsonStr)
        {
            var _config = jsonStr.DeJson<Config>();

            ListenEnable = _config.ListenEnable;
            ProxyAll = _config.ProxyAll;
            ConnectEnable = _config.ConnectEnable;
            await configDataProvider.Save(jsonStr).ConfigureAwait(false);

        }
        public async Task SaveConfig()
        {
            await configDataProvider.Save(this).ConfigureAwait(false);
        }
    }
}
