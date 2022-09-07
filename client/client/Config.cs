using common.libs.database;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace client
{
    /// <summary>
    /// 配置信息
    /// </summary>
    [Table("appsettings")]
    public class Config
    {
        public Config() { }
        private readonly IConfigDataProvider<Config> configDataProvider;
        public Config(IConfigDataProvider<Config> configDataProvider)
        {
            this.configDataProvider = configDataProvider;

            Config config = ReadConfig().Result;
            Client = config.Client;
            Server = config.Server;
        }
        /// <summary>
        /// 客户端配置
        /// </summary>
        public ClientConfig Client { get; set; } = new ClientConfig();
        /// <summary>
        /// 服务器配置
        /// </summary>
        public ServerConfig Server { get; set; } = new ServerConfig();

        public async Task<Config> ReadConfig()
        {
            return await configDataProvider.Load();
        }

        public async Task SaveConfig()
        {
            Config config = await ReadConfig().ConfigureAwait(false);

            config.Client = Client;
            config.Server = Server;

            await configDataProvider.Save(config).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 客户端配置
    /// </summary>
    public class ClientConfig
    {
        /// <summary>
        /// 分组编号
        /// </summary>
        public string GroupId { get; set; } = string.Empty;
        /// <summary>
        /// 客户端名
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// 自动注册
        /// </summary>
        public bool AutoReg { get; set; } = false;
        /// <summary>
        /// 自动注册重试次数
        /// </summary>
        public int AutoRegTimes { get; set; } = 10;
        public int AutoRegInterval { get; set; } = 5000;
        public int AutoRegDelay { get; set; } = 5000;


        /// <summary>
        /// 上报MAC地址
        /// </summary>
        public bool UseMac { get; set; } = false;
        /// <summary>
        /// 使用ipv6
        /// </summary>
        public bool UseIpv6 { get; set; } = false;

        public int TcpBufferSize { get; set; } = 128 * 1024;

        public string Key { get; set; } = string.Empty;

        public bool Encode { get; set; } = false;

        public string EncodePassword { get; set; } = string.Empty;

        public bool AutoPunchHole { get; set; } = true;
        public bool UseUdp { get; set; } = true;
        public bool UseTcp { get; set; } = true;


        

        [JsonIgnore]
        public IPAddress BindIp
        {
            get
            {
                return UseIpv6 ? IPAddress.IPv6Any : IPAddress.Any;
            }
        }

        [JsonIgnore]
        public IPAddress LoopbackIp
        {
            get
            {
                return UseIpv6 ? IPAddress.IPv6Loopback : IPAddress.Loopback;
            }
        }
    }

    /// <summary>
    /// 服务器配置
    /// </summary>
    public class ServerConfig
    {
        public string Ip { get; set; } = string.Empty;
        public int UdpPort { get; set; } = 8099;
        public int TcpPort { get; set; } = 8000;
        public bool Encode { get; set; } = false;
        public string EncodePassword { get; set; } = string.Empty;
    }
}
