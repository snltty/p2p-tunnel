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
        public Config() { }
        private readonly IConfigDataProvider<Config> configDataProvider;
        private readonly ISocks5ClientListener socks5ClientListener;
        public Config(IConfigDataProvider<Config> configDataProvider, ISocks5ClientListener socks5ClientListener)
        {
            this.configDataProvider = configDataProvider;
            this.socks5ClientListener = socks5ClientListener;

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

        public bool ListenEnable { get; set; } = false;
        public int ListenPort { get; set; } = 5412;
        public int BufferSize { get; set; } = 8 * 1024;
        public bool ConnectEnable { get; set; } = false;
        public bool IsCustomPac { get; set; } = false;
        public bool IsPac { get; set; } = false;
        public string TargetName { get; set; } = string.Empty;
        public int NumConnections { get; set; } = 1000;

        public async Task<Config> ReadConfig()
        {
            var config =  await configDataProvider.Load();
            return config;
        }

        public async Task<string> ReadString()
        {
            return await configDataProvider.LoadString();
        }

        public async Task SaveConfig(string jsonStr)
        {
            Config config = jsonStr.DeJson<Config>();
            ListenEnable = config.ListenEnable;
            ListenPort = config.ListenPort;
            BufferSize = config.BufferSize;
            ConnectEnable = config.ConnectEnable;
            IsCustomPac = config.IsCustomPac;
            IsPac = config.IsPac;
            TargetName = config.TargetName;
            NumConnections = config.NumConnections;

            socks5ClientListener?.SetBufferSize(BufferSize);
            await configDataProvider.Save(jsonStr).ConfigureAwait(false);
        }
    }
}
