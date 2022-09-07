using common.libs.database;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;

namespace common.udpforward
{
    [Table("udpforward-appsettings")]
    public class Config
    {
        public Config() { }
        private readonly IConfigDataProvider<Config> configDataProvider;
        public Config(IConfigDataProvider<Config> configDataProvider)
        {
            this.configDataProvider = configDataProvider;

            Config config = ReadConfig().Result;
            ConnectEnable = config.ConnectEnable;
            LanConnectEnable = config.LanConnectEnable;
            TunnelListenRange = config.TunnelListenRange;
            PortWhiteList = config.PortWhiteList;
            PortBlackList = config.PortBlackList;

        }

        public int[] PortWhiteList { get; set; } = Array.Empty<int>();
        public int[] PortBlackList { get; set; } = Array.Empty<int>();
        public bool ConnectEnable { get; set; } = false;
        public bool LanConnectEnable { get; set; } = false;
        public TunnelListenRangeInfo TunnelListenRange { get; set; } = new TunnelListenRangeInfo();

        public async Task<Config> ReadConfig()
        {
            return await configDataProvider.Load();
        }

        public async Task SaveConfig()
        {
            Config config = await ReadConfig().ConfigureAwait(false);
            config.ConnectEnable = ConnectEnable;
            config.LanConnectEnable = LanConnectEnable;
            config.TunnelListenRange = TunnelListenRange;
            config.PortWhiteList = PortWhiteList;
            config.PortBlackList = PortBlackList;

            await configDataProvider.Save(config).ConfigureAwait(false);
        }
    }

    public class TunnelListenRangeInfo
    {
        public int Min { get; set; } = 10000;
        public int Max { get; set; } = 60000;
    }
}
