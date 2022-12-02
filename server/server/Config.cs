using common.libs.database;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;

namespace server
{
    [Table("appsettings")]
    public sealed class Config
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
            RegisterEnable = config.RegisterEnable;
            RelayEnable = config.RelayEnable;
            EncodePassword = config.EncodePassword;
        }

        public int Udp { get; set; } = 0;
        public int Tcp { get; set; } = 0;
        public int ConnectLimit { get; set; } = 0;
        public int TcpBufferSize { get; set; } = 8 * 1024;
        public int TimeoutDelay { get; set; } = 20 * 1000;
        public int RegisterTimeout { get; set; } = 5000;


        public bool RegisterEnable { get; set; } = true;
        public bool RelayEnable { get; set; } = false;
        public string EncodePassword { get; set; } = string.Empty;


        private async Task<Config> ReadConfig()
        {
            return await configDataProvider.Load();
        }

        public async Task<string> ReadString()
        {
            return await configDataProvider.LoadString();
        }

        public async Task SaveConfig(string jsonStr)
        {
            Config config = await ReadConfig().ConfigureAwait(false);

            config.Udp = Udp;
            config.Tcp = Tcp;
            config.ConnectLimit = ConnectLimit;
            config.TcpBufferSize = TcpBufferSize;
            config.TimeoutDelay = TimeoutDelay;
            config.RegisterTimeout = RegisterTimeout;
            config.RegisterEnable = RegisterEnable;
            config.RelayEnable = RelayEnable;
            config.EncodePassword = EncodePassword;

            await configDataProvider.Save(jsonStr).ConfigureAwait(false);
        }
    }

}
