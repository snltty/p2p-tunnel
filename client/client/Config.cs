using common.libs;
using common.libs.database;
using System;
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
    public sealed class Config
    {
        public Config() { }
        private readonly IConfigDataProvider<Config> configDataProvider;
        public Config(IConfigDataProvider<Config> configDataProvider)
        {
            this.configDataProvider = configDataProvider;

            Config config = ReadConfig().Result;
            Client = config.Client;
            Server = config.Server;

            if (Client.Name.Length == 0)
            {
                Client.Name = $"{Environment.MachineName}_{Environment.UserName}";
            }
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
        public async Task<string> ReadString()
        {
            return await configDataProvider.LoadString();
        }

        public async Task SaveConfig(string jsonStr)
        {
            Config config = await ReadConfig().ConfigureAwait(false);

            config.Client = Client;
            config.Server = Server;

            await configDataProvider.Save(jsonStr).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 客户端配置
    /// </summary>
    public sealed class ClientConfig
    {
        public byte ShortId { get; set; }
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
        /// 使用ipv6
        /// </summary>
        public bool UseIpv6 { get; set; } = true;

        public int TcpBufferSize { get; set; } = 128 * 1024;

        public bool Encode { get; set; } = false;

        public string EncodePassword { get; set; } = string.Empty;

        public int TimeoutDelay { get; set; } = 20000;

        public bool UsePunchHole { get; set; } = true;
        public bool UseUdp { get; set; } = true;
        public bool UseTcp { get; set; } = true;
        public bool UseRelay { get; set; } = true;
        public bool UseOriginPort { get; set; } = true;
        public bool UseReConnect { get; set; } = true;

        public int UdpUploadSpeedLimit { get; set; } = 0;


        [JsonIgnore]
        public IPAddress BindIp
        {
            get
            {
                return UseIpv6 && NetworkHelper.IPv6Support ? IPAddress.IPv6Any : IPAddress.Any;
            }
        }

        [JsonIgnore]
        public IPAddress LoopbackIp
        {
            get
            {
                return UseIpv6 && NetworkHelper.IPv6Support ? IPAddress.IPv6Loopback : IPAddress.Loopback;
            }
        }

        public EnumClientAccess GetAccess()
        {
            return EnumClientAccess.None
                | (UseUdp ? EnumClientAccess.UseUdp : EnumClientAccess.None)
                | (UseTcp ? EnumClientAccess.UseTcp : EnumClientAccess.None)
                | (UsePunchHole ? EnumClientAccess.UsePunchHole : EnumClientAccess.None)
                | (UseRelay ? EnumClientAccess.UseRelay : EnumClientAccess.None);

        }
    }

    /// <summary>
    /// 客户端权限类别
    /// </summary>
    [Flags]
    public enum EnumClientAccess : uint
    {
        None = 0,
        UseUdp = 1,
        UseTcp = 2,
        UsePunchHole = 4,
        UseRelay = 8,
        All = 0xffffffff
    }

    /// <summary>
    /// 服务器配置
    /// </summary>
    public sealed class ServerConfig
    {
        public string Ip { get; set; } = string.Empty;
        public int UdpPort { get; set; } = 8099;
        public int TcpPort { get; set; } = 8000;
        public bool Encode { get; set; } = false;
        public string EncodePassword { get; set; } = string.Empty;
    }
}
