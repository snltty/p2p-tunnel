using common.libs.database;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;

namespace common.tcpforward
{
    [Table("tcpforward-appsettings")]
    public class Config
    {
        public Config() { }
        private readonly IConfigDataProvider<Config> configDataProvider;
        public Config(IConfigDataProvider<Config> configDataProvider)
        {
            this.configDataProvider = configDataProvider;

            Config config = ReadConfig().Result;
            ConnectEnable = config.ConnectEnable;
            NumConnections = config.NumConnections;
            BufferSize = config.BufferSize;
            WebListens = config.WebListens;
            TunnelListenRange = config.TunnelListenRange;
            PortWhiteList = config.PortWhiteList;
            PortBlackList = config.PortBlackList;
            LanConnectEnable = config.LanConnectEnable;
            
        }

        public int[] PortWhiteList { get; set; } = Array.Empty<int>();
        public int[] PortBlackList { get; set; } = Array.Empty<int>();
        
        public bool LanConnectEnable { get; set; } = false;
        public bool ConnectEnable { get; set; } = false;
        public int NumConnections { get; set; } = 1000;
        public int BufferSize { get; set; } = 8 * 1024;
        public int[] WebListens { get; set; } = Array.Empty<int>();
        public TunnelListenRangeInfo TunnelListenRange { get; set; } = new TunnelListenRangeInfo();

        public async Task<Config> ReadConfig()
        {
            return await configDataProvider.Load();
        }

        public async Task SaveConfig()
        {
            Config config = await ReadConfig().ConfigureAwait(false);
            config.LanConnectEnable = LanConnectEnable;
            config.ConnectEnable = ConnectEnable;
            config.NumConnections = NumConnections;
            config.BufferSize = BufferSize;
            config.WebListens = WebListens;
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
