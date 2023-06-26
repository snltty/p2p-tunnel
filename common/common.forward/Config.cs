using common.libs;
using common.libs.database;
using common.libs.extends;
using common.proxy;
using common.server.model;
using System;
using System.Buffers.Binary;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Net;
using System.Text.Json.Serialization;
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
            SaveConfig().Wait();
        }

        [JsonIgnore]
        public byte Plugin => 1;

        /// <summary>
        /// 允许连接
        /// </summary>
        public bool ConnectEnable { get; set; } = false;
        public EnumBufferSize BufferSize { get; set; } = EnumBufferSize.KB_8;
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
            WebListens = _config.WebListens;
            TunnelListenRange = _config.TunnelListenRange;

            await configDataProvider.Save(jsonStr).ConfigureAwait(false);
        }
        public async Task SaveConfig()
        {
            await configDataProvider.Save(this).ConfigureAwait(false);
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

    public sealed class LanIPAddress
    {
        /// <summary>
        /// ip，存小端
        /// </summary>
        public uint IPAddress { get; set; }
        public byte MaskLength { get; set; }
        public uint MaskValue { get; set; }
        public uint NetWork { get; set; }
    }

}
