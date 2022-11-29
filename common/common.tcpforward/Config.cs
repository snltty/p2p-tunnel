using common.libs.database;
using common.libs.extends;
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

        }

        public ushort[] PortWhiteList { get; set; } = Array.Empty<ushort>();
        public ushort[] PortBlackList { get; set; } = Array.Empty<ushort>();

        public bool ConnectEnable { get; set; } = false;
        public int NumConnections { get; set; } = 1000;
        public int BufferSize { get; set; } = 8 * 1024;
        public ushort[] WebListens { get; set; } = Array.Empty<ushort>();
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
            NumConnections = _config.NumConnections;
            BufferSize = _config.BufferSize;
            WebListens = _config.WebListens;
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
