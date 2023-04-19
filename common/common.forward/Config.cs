using common.libs.database;
using common.libs.extends;
using common.proxy;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;

namespace common.forward
{
    /// <summary>
    /// tcp转发配置文件
    /// </summary>
    [Table("forward-appsettings")]
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
            WebListens = config.WebListens;
            TunnelListenRange = config.TunnelListenRange;
            PortWhiteList = config.PortWhiteList;
            PortBlackList = config.PortBlackList;

        }

        [System.Text.Json.Serialization.JsonIgnore]
        public byte Plugin => 3;

        /// <summary>
        /// 端口白名单
        /// </summary>
        public ushort[] PortWhiteList { get; set; } = Array.Empty<ushort>();
        /// <summary>
        /// 端口黑名单
        /// </summary>
        public ushort[] PortBlackList { get; set; } = Array.Empty<ushort>();
        /// <summary>
        /// 允许连接
        /// </summary>
        public bool ConnectEnable { get; set; } = false;
        public EnumProxyBufferSize BufferSize { get; set; } = EnumProxyBufferSize.KB_8;
        /// <summary>
        /// 短连接端口
        /// </summary>
        public ushort[] WebListens { get; set; } = Array.Empty<ushort>();
        /// <summary>
        /// 域名列表
        /// </summary>
        public string[] Domains { get; set; } = Array.Empty<string>();
        /// <summary>
        /// 长链接端口范围
        /// </summary>
        public TunnelListenRangeInfo TunnelListenRange { get; set; } = new TunnelListenRangeInfo();
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
            WebListens = _config.WebListens;
            TunnelListenRange = _config.TunnelListenRange;
            PortWhiteList = _config.PortWhiteList;
            PortBlackList = _config.PortBlackList;

            await configDataProvider.Save(jsonStr).ConfigureAwait(false);
        }
    }
    /// <summary>
    /// 长链接端口范围
    /// </summary>
    public sealed class TunnelListenRangeInfo
    {
        /// <summary>
        /// 最小
        /// </summary>
        public ushort Min { get; set; } = 10000;
        /// <summary>
        /// 最大
        /// </summary>
        public ushort Max { get; set; } = 60000;
    }

}
