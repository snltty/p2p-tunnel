using common.libs.database;
using common.libs.extends;
using common.proxy;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;

namespace common.httpProxy
{
    /// <summary>
    /// tcp转发配置文件
    /// </summary>
    [Table("httpproxy-appsettings")]
    public sealed class Config
    {
        public Config() { }
        private readonly IConfigDataProvider<Config> configDataProvider;

        public Config(IConfigDataProvider<Config> configDataProvider)
        {
            this.configDataProvider = configDataProvider;

            Config config = ReadConfig().Result;
            ConnectEnable = config.ConnectEnable;
            BufferSize = config.BufferSize;
        }

        [System.Text.Json.Serialization.JsonIgnore]
        public byte Plugin => 4;


        public bool IsPac { get; set; } = false;
        public bool IsCustomPac { get; set; } = false;
        public string TargetName { get; set; } = string.Empty;
        

        /// <summary>
        /// 开启监听
        /// </summary>
        public bool ListenEnable { get; set; } = false;
        /// <summary>
        /// 监听端口
        /// </summary>
        public ushort ListenPort { get; set; } = 5412;
        /// <summary>
        /// 允许连接
        /// </summary>
        public bool ConnectEnable { get; set; } = false;
        public EnumProxyBufferSize BufferSize { get; set; } = EnumProxyBufferSize.KB_8;
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
            var _config = jsonStr.DeJson<Config>();

            ConnectEnable = _config.ConnectEnable;
            BufferSize = _config.BufferSize;

            await configDataProvider.Save(jsonStr).ConfigureAwait(false);
        }
    }
}
