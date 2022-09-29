using common.libs.database;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;

namespace server
{
    [Table("appsettings")]
    public class Config
    {
        public Config() { }
        private readonly IConfigDataProvider<Config> configDataProvider;
        public Config(IConfigDataProvider<Config> configDataProvider)
        {
            this.configDataProvider = configDataProvider;

            Config config = ReadConfig().Result;
            Udp = config.Udp;
            Tcp = config.Tcp;
            ConnectLimit = config.ConnectLimit;
            TcpBufferSize = config.TcpBufferSize;
            TimeoutDelay = config.TimeoutDelay;
            RegisterTimeout = config.RegisterTimeout;
            Relay = config.Relay;
            EncodePassword = config.EncodePassword;
        }

        public int Udp { get; set; } = 0;
        public int Tcp { get; set; } = 0;
        public int ConnectLimit { get; set; } = 0;
        public int TcpBufferSize { get; set; } = 8 * 1024;
        public int TimeoutDelay { get; set; } = 20 * 1000;
        public int RegisterTimeout { get; set; } = 5000;
        
        public bool Relay { get; set; } = false;
        public string EncodePassword { get; set; } = string.Empty;


        private async Task<Config> ReadConfig()
        {
            return await configDataProvider.Load();
        }

        private async Task SaveConfig()
        {
            Config config = await ReadConfig().ConfigureAwait(false);

            config.Udp = Udp;
            config.Tcp = Tcp;
            config.ConnectLimit = ConnectLimit;
            config.TcpBufferSize = TcpBufferSize;
            config.TimeoutDelay = TimeoutDelay;
            config.RegisterTimeout = RegisterTimeout;
            config.Relay = Relay;
            config.EncodePassword = EncodePassword;

            await configDataProvider.Save(config).ConfigureAwait(false);
        }
    }


    [Table("keys")]
    public class KeysConfig
    {
        public KeysConfig() { }
        private readonly IConfigDataProvider<KeysConfig> configDataProvider;
        public KeysConfig(IConfigDataProvider<KeysConfig> configDataProvider)
        {
            this.configDataProvider = configDataProvider;

            KeysConfig config = ReadConfig().Result;
            if (config != null)
            {
                Keys = config.Keys;
                UdpForward = config.UdpForward;
                TcpForward = config.TcpForward;
                Socks5 = config.Socks5;
            }
        }

        public string[] Keys { get; set; } = Array.Empty<string>();
        public string[] UdpForward { get; set; } = Array.Empty<string>();
        public string[] TcpForward { get; set; } = Array.Empty<string>();
        public string[] Socks5 { get; set; } = Array.Empty<string>();

        private async Task<KeysConfig> ReadConfig()
        {
            return await configDataProvider.Load();
        }
    }
}
