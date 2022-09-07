using common.libs.database;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;
using System.Threading.Tasks;

namespace client.service.ui.api
{
    /// <summary>
    /// 配置信息
    /// </summary>
    [Table("ui-appsettings")]
    public class Config
    {
        public Config() { }
        private readonly IConfigDataProvider<Config> configDataProvider;
        public Config(IConfigDataProvider<Config> configDataProvider)
        {
            this.configDataProvider = configDataProvider;

            Config config = ReadConfig().Result;
            EnableWeb = config.EnableWeb;
            EnableCommand = config.EnableCommand;
            EnableApi = config.EnableApi;
            Websocket = config.Websocket;
            Web = config.Web;
        }

        public bool EnableWeb { get; set; } = true;
        public bool EnableCommand { get; set; } = true;
        public bool EnableApi { get; set; } = true;

        /// <summary>
        /// 本地websocket
        /// </summary>
        public WebsocketConfig Websocket { get; set; } = new WebsocketConfig();
        /// <summary>
        /// 本地web管理端配置
        /// </summary>
        public WebConfig Web { get; set; } = new WebConfig();


        public async Task<Config> ReadConfig()
        {
            return await configDataProvider.Load();
        }

        public async Task SaveConfig()
        {
            Config config = await ReadConfig().ConfigureAwait(false);

            config.EnableWeb = EnableWeb;
            config.EnableCommand = EnableCommand;
            config.EnableApi = EnableApi;
            config.Web = Web;
            config.Websocket = Websocket;

            await configDataProvider.Save(config).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 本地web管理端配置
    /// </summary>
    public class WebConfig
    {
        public int Port { get; set; } = 8098;
        public string Root { get; set; } = "./web";
        public IPAddress BindIp { get; set; } = IPAddress.Loopback;

    }
    public class WebsocketConfig
    {
        public int Port { get; set; } = 8098;
        public IPAddress BindIp { get; set; } = IPAddress.Loopback;
    }
}
