using common.libs.database;
using common.libs.extends;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;

namespace client.service.logger
{
    /// <summary>
    /// 日志配置文件
    /// </summary>
    [Table("logger-appsettings")]
    public sealed class Config
    {
        /// <summary>
        /// 
        /// </summary>
        public Config() { }
        private readonly IConfigDataProvider<Config> configDataProvider;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="configDataProvider"></param>
        public Config(IConfigDataProvider<Config> configDataProvider)
        {
            this.configDataProvider = configDataProvider;

            Config config = ReadConfig().Result;
            Enable = config.Enable;
            MaxLength = config.MaxLength;
        }


        /// <summary>
        /// 开启
        /// </summary>
        public bool Enable { get; set; } = false;
        /// <summary>
        /// 行数
        /// </summary>
        public int MaxLength { get; set; } = 100;

        /// <summary>
        /// 读取
        /// </summary>
        /// <returns></returns>
        public async Task<Config> ReadConfig()
        {
            return await configDataProvider.Load();
        }
        /// <summary>
        /// 读取
        /// </summary>
        /// <returns></returns>
        public async Task<string> ReadString()
        {
            return await configDataProvider.LoadString();
        }

        /// <summary>
        /// 保存
        /// </summary>
        /// <param name="jsonStr"></param>
        /// <returns></returns>
        public async Task SaveConfig(string jsonStr)
        {
            Config _config = jsonStr.DeJson<Config>();

            Enable = _config.Enable;
            MaxLength = _config.MaxLength;
            await configDataProvider.Save(jsonStr).ConfigureAwait(false);
        }
    }
}
