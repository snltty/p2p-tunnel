using common.libs.database;
using common.libs.extends;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;

namespace common.udpforward
{
    [Table("udpforward-appsettings")]
    public sealed class Config
    {
        public Config() { }
        private readonly IConfigDataProvider<Config> configDataProvider;
        public Config(IConfigDataProvider<Config> configDataProvider)
        {
            this.configDataProvider = configDataProvider;

            Config config = ReadConfig().Result;
            ConnectEnable = config.ConnectEnable;
            TunnelListenRange = config.TunnelListenRange;
            PortWhiteList = config.PortWhiteList;
            PortBlackList = config.PortBlackList;

        }

        public int[] PortWhiteList { get; set; } = Array.Empty<int>();
        public int[] PortBlackList { get; set; } = Array.Empty<int>();
        public bool ConnectEnable { get; set; } = false;
        public TunnelListenRangeInfo TunnelListenRange { get; set; } = new TunnelListenRangeInfo();

        public async Task<Config> ReadConfig()
        {
            return await configDataProvider.Load();
        }
        public async Task<string> ReadString()
        {
            return await configDataProvider.LoadString();
        }

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

    public class TunnelListenRangeInfo
    {
        public ushort Min { get; set; } = 10000;
        public ushort Max { get; set; } = 60000;
    }
}
