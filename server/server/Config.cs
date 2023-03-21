using common.libs.database;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;

namespace server
{
    /// <summary>
    /// 服务端配置文件
    /// </summary>
    [Table("appsettings")]
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

        /// <summary>
        /// udp端口
        /// </summary>
        public int Udp { get; set; } = 0;
        /// <summary>
        /// tcp端口
        /// </summary>
        public int Tcp { get; set; } = 0;
        /// <summary>
        /// 连接频率每秒
        /// </summary>
        public int ConnectLimit { get; set; } = 0;
        /// <summary>
        /// 
        /// </summary>
        public int TcpBufferSize { get; set; } = 8 * 1024;
        /// <summary>
        /// 掉线超时
        /// </summary>
        public int TimeoutDelay { get; set; } = 20 * 1000;
        /// <summary>
        /// 注册超时
        /// </summary>
        public int RegisterTimeout { get; set; } = 5000;

        /// <summary>
        /// 验证账号
        /// </summary>
        public bool VerifyAccount { get; set; } = false;
        /// <summary>
        /// 允许注册
        /// </summary>
        public bool RegisterEnable { get; set; } = true;
        /// <summary>
        /// 允许中继
        /// </summary>
        public bool RelayEnable { get; set; } = false;
        /// <summary>
        /// 加密密码
        /// </summary>
        public string EncodePassword { get; set; } = string.Empty;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task<Config> ReadConfig()
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
