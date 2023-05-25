using common.libs.database;
using common.libs.extends;
using common.proxy;
using common.server.model;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;
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
            ProxyIp = config.ProxyIp;
            TargetConnectionId = config.TargetConnectionId;

        }

        [System.Text.Json.Serialization.JsonIgnore]
        public byte Plugin => 4;

        public bool ListenEnable { get; set; }
        public int ListenPort { get; set; } = 5412;
        public EnumBufferSize BufferSize { get; set; } = EnumBufferSize.KB_8;
        public bool ConnectEnable { get; set; } 
        public bool IsCustomPac { get; set; } 
        public bool IsPac { get; set; }
        public IPAddress ProxyIp { get; set; } = IPAddress.Loopback;
        public ulong TargetConnectionId { get; set; }

        public async Task<Config> ReadConfig()
        {
            var config = await configDataProvider.Load();
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
            ProxyIp = config.ProxyIp;
            TargetConnectionId = config.TargetConnectionId;
            await configDataProvider.Save(jsonStr).ConfigureAwait(false);
        }
    }
}
