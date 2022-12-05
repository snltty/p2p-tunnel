using common.libs.database;
using common.libs.extends;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;

namespace common.socks5
{
    /// <summary>
    /// socks5配置文件
    /// </summary>
    [Table("socks5-appsettings")]
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
            ListenEnable = config.ListenEnable;
            ListenPort = config.ListenPort;
            BufferSize = config.BufferSize;
            ConnectEnable = config.ConnectEnable;
            IsCustomPac = config.IsCustomPac;
            IsPac = config.IsPac;
            TargetName = config.TargetName;
            NumConnections = config.NumConnections;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool ListenEnable { get; set; } = false;
        /// <summary>
        /// 
        /// </summary>
        public int ListenPort { get; set; } = 5412;
        /// <summary>
        /// 
        /// </summary>
        public int BufferSize { get; set; } = 8 * 1024;
        /// <summary>
        /// 
        /// </summary>
        public bool ConnectEnable { get; set; } = false;
        /// <summary>
        /// 
        /// </summary>
        public bool IsCustomPac { get; set; } = false;
        /// <summary>
        /// 
        /// </summary>
        public bool IsPac { get; set; } = false;
        /// <summary>
        /// 
        /// </summary>
        public string TargetName { get; set; } = string.Empty;
        /// <summary>
        /// 
        /// </summary>
        public int NumConnections { get; set; } = 1000;
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<Config> ReadConfig()
        {
            return await configDataProvider.Load();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<string> ReadString()
        {
            return await configDataProvider.LoadString();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="jsonStr"></param>
        /// <returns></returns>
        public async Task SaveConfig(string jsonStr)
        {
            Config config = jsonStr.DeJson<Config>();
            config.ListenEnable = ListenEnable;
            config.ListenPort = ListenPort;
            config.BufferSize = BufferSize;
            config.ConnectEnable = ConnectEnable;
            config.IsCustomPac = IsCustomPac;
            config.IsPac = IsPac;
            config.TargetName = TargetName;
            config.NumConnections = NumConnections;
            
            await configDataProvider.Save(jsonStr).ConfigureAwait(false);
        }
    }

}
