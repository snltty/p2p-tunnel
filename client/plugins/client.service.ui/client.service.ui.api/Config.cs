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
            EnableWeb = config.EnableWeb;
            EnableCommand = config.EnableCommand;
            EnableApi = config.EnableApi;
            Websocket = config.Websocket;
            Web = config.Web;
        }
        /// <summary>
        /// 启用web
        /// </summary>
        public bool EnableWeb { get; set; } = true;
        /// <summary>
        /// 启用命令行，具名管道
        /// </summary>
        public bool EnableCommand { get; set; } = true;
        /// <summary>
        /// 启用websocket
        /// </summary>
        public bool EnableApi { get; set; } = true;

        /// <summary>
        /// 本地websocket
        /// </summary>
        public WebsocketConfig Websocket { get; set; } = new WebsocketConfig();
        /// <summary>
        /// 本地web管理端配置
        /// </summary>
        public WebConfig Web { get; set; } = new WebConfig();

        /// <summary>
        /// 读取配置
        /// </summary>
        /// <returns></returns>
        public async Task<Config> ReadConfig()
        {
            return await configDataProvider.Load();
        }
        /// <summary>
        /// 读取配置
        /// </summary>
        /// <returns></returns>
        public async Task<string> ReadString()
        {
            return await configDataProvider.LoadString();
        }
        /// <summary>
        /// 保存
        /// </summary>
        /// <param name="jsonStr"></param>
        /// <returns></returns>
        public async Task SaveConfig(string jsonStr)
        {
            Config config = await ReadConfig().ConfigureAwait(false);

            config.EnableWeb = EnableWeb;
            config.EnableCommand = EnableCommand;
            config.EnableApi = EnableApi;
            config.Web = Web;
            config.Websocket = Websocket;

            await configDataProvider.Save(jsonStr).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 本地web管理端配置
    /// </summary>
    public class WebConfig
    {
        /// <summary>
        /// 端口
        /// </summary>
        public int Port { get; set; } = 8098;
        /// <summary>
        /// 根目录
        /// </summary>
        public string Root { get; set; } = "./web";
        /// <summary>
        /// 绑定ip
        /// </summary>
        public string BindIp { get; set; } = "+";

    }
    /// <summary>
    /// 本地websocket
    /// </summary>
    public class WebsocketConfig
    {
        /// <summary>
        /// 端口
        /// </summary>
        public int Port { get; set; } = 8098;
        /// <summary>
        /// 绑定ip
        /// </summary>
        public IPAddress BindIp { get; set; } = IPAddress.Loopback;
    }
}
