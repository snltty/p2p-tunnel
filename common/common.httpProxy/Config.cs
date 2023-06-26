using common.libs.database;
using common.libs.extends;
using common.proxy;
using common.server.model;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;
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
            ListenEnable = config.ListenEnable;
            ListenPort = config.ListenPort;
            IsPac = config.IsPac;
            ProxyIp = config.ProxyIp;
            IsCustomPac = config.IsCustomPac;
            TargetConnectionId = config.TargetConnectionId;
            SaveConfig().Wait();
        }

        [System.Text.Json.Serialization.JsonIgnore]
        public byte Plugin => 2;

        public IPAddress ProxyIp { get; set; } = IPAddress.Loopback;
        public bool IsPac { get; set; }
        public bool IsCustomPac { get; set; }
        public ulong TargetConnectionId { get; set; }


        /// <summary>
        /// 开启监听
        /// </summary>
        public bool ListenEnable { get; set; }
        /// <summary>
        /// 监听端口
        /// </summary>
        public ushort ListenPort { get; set; } = 5412;
        /// <summary>
        /// 允许连接
        /// </summary>
        public bool ConnectEnable { get; set; }
        public EnumBufferSize BufferSize { get; set; } = EnumBufferSize.KB_8;

        /// <summary>
        /// 读取
        /// </summary>
        /// <returns></returns>
        public async Task<Config> ReadConfig()
        {
            return await configDataProvider.Load() ?? new Config();
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
            ListenEnable = _config.ListenEnable;
            ListenPort = _config.ListenPort;
            IsPac = _config.IsPac;
            ProxyIp = _config.ProxyIp;
            IsCustomPac = _config.IsCustomPac;
            TargetConnectionId = _config.TargetConnectionId;

            await configDataProvider.Save(jsonStr).ConfigureAwait(false);
        }
        public async Task SaveConfig()
        {
            await configDataProvider.Save(this).ConfigureAwait(false);
        }
    }
}
