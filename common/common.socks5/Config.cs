using common.libs.database;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;

namespace common.socks5
{
    [Table("socks5-appsettings")]
    public class Config
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
            TargetName = config.TargetName;
            TunnelType = config.TunnelType;
            LanConnectEnable = config.LanConnectEnable;
            NumConnections = config.NumConnections;
        }

        public bool ListenEnable { get; set; } = false;
        public int ListenPort { get; set; } = 5412;
        public int BufferSize { get; set; } = 8 * 1024;
        public bool ConnectEnable { get; set; } = false;
        public bool IsCustomPac { get; set; } = false;
        public bool IsPac { get; set; } = false;
        public string TargetName { get; set; } = string.Empty;
        public TunnelTypes TunnelType { get; set; } = TunnelTypes.TCP_FIRST;
        public bool LanConnectEnable { get; set; } = false;
        public int NumConnections { get; set; } = 1000;
        

        public async Task<Config> ReadConfig()
        {
            return await configDataProvider.Load();
        }

        public async Task SaveConfig()
        {
            Config config = await ReadConfig().ConfigureAwait(false);
            config.ListenEnable = ListenEnable;
            config.ListenPort = ListenPort;
            config.BufferSize = BufferSize;
            config.ConnectEnable = ConnectEnable;
            config.IsCustomPac = IsCustomPac;
            config.IsPac = IsPac;
            config.TargetName = TargetName;
            config.TunnelType = TunnelType;
            config.LanConnectEnable = LanConnectEnable;
            config.NumConnections = NumConnections;
            
            await configDataProvider.Save(config).ConfigureAwait(false);
        }
    }

    [Flags]
    public enum TunnelTypes : byte
    {
        [Description("只tcp")]
        TCP = 1 << 1,
        [Description("只udp")]
        UDP = 1 << 2,
        [Description("优先tcp")]
        TCP_FIRST = 1 << 3,
        [Description("优先udp")]
        UDP_FIRST = 1 << 4,
    }
}
