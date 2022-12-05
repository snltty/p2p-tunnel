using common.libs.database;
using common.libs.extends;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;

namespace common.udpforward
{
    /// <summary>
    /// udp转发配置文件
    /// </summary>
    [Table("udpforward-appsettings")]
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
            ConnectEnable = config.ConnectEnable;
            TunnelListenRange = config.TunnelListenRange;
            PortWhiteList = config.PortWhiteList;
            PortBlackList = config.PortBlackList;

        }
        /// <summary>
        /// 
        /// </summary>
        public int[] PortWhiteList { get; set; } = Array.Empty<int>();
        /// <summary>
        /// 
        /// </summary>
        public int[] PortBlackList { get; set; } = Array.Empty<int>();
        /// <summary>
        /// 
        /// </summary>
        public bool ConnectEnable { get; set; } = false;
        /// <summary>
        /// 
        /// </summary>
        public TunnelListenRangeInfo TunnelListenRange { get; set; } = new TunnelListenRangeInfo();
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
            var _config = jsonStr.DeJson<Config>();

            ConnectEnable = _config.ConnectEnable;
            TunnelListenRange = _config.TunnelListenRange;
            PortWhiteList = _config.PortWhiteList;
            PortBlackList = _config.PortBlackList;


            await configDataProvider.Save(jsonStr).ConfigureAwait(false);
        }
    }
    /// <summary>
    /// 
    /// </summary>
    public sealed class TunnelListenRangeInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public ushort Min { get; set; } = 10000;
        /// <summary>
        /// 
        /// </summary>
        public ushort Max { get; set; } = 60000;
    }
}
