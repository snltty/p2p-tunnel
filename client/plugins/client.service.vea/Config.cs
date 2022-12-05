using common.libs.database;
using common.libs.extends;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;
using System.Threading.Tasks;

namespace client.service.vea
{
    /// <summary>
    /// 组网配置文件
    /// </summary>
    [Table("vea-appsettings")]
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
            Enable = config.Enable;
            ProxyAll = config.ProxyAll;
            TargetName = config.TargetName;
            IP = config.IP;
            LanIPs = config.LanIPs;
            SocksPort = config.SocksPort;
            BufferSize = config.BufferSize;
            NumConnections = config.NumConnections;
            ConnectEnable = config.ConnectEnable;
        }

        /// <summary>
        /// 启用
        /// </summary>
        public bool Enable { get; set; } = false;
        /// <summary>
        /// 代理所有
        /// </summary>
        public bool ProxyAll { get; set; } = false;
        /// <summary>
        /// 目标，当遇到不存在的ip时
        /// </summary>
        public string TargetName { get; set; } = string.Empty;
        /// <summary>
        /// 组网ip
        /// </summary>
        public IPAddress IP { get; set; } = IPAddress.Parse("192.168.54.1");
        /// <summary>
        /// 局域网网段ip列表
        /// </summary>
        public IPAddress[] LanIPs { get; set; } = Array.Empty<IPAddress>();

        /// <summary>
        /// 监听端口
        /// </summary>
        public int SocksPort { get; set; } = 5415;
        /// <summary>
        /// buffersize
        /// </summary>
        public int BufferSize { get; set; } = 8 * 1024;
        /// <summary>
        /// 连接数
        /// </summary>
        public int NumConnections { get; set; } = 1000;
        /// <summary>
        /// 允许被连接
        /// </summary>
        public bool ConnectEnable { get; set; } = false;

        /// <summary>
        /// 读取配置文件
        /// </summary>
        /// <returns></returns>
        public async Task<Config> ReadConfig()
        {
            return await configDataProvider.Load();
        }
        /// <summary>
        /// 读取配置文件
        /// </summary>
        /// <returns></returns>
        public async Task<string> ReadString()
        {
            return await configDataProvider.LoadString();
        }
        /// <summary>
        /// 保存配置文件
        /// </summary>
        /// <param name="jsonStr"></param>
        /// <returns></returns>
        public async Task SaveConfig(string jsonStr)
        {
            var _config = jsonStr.DeJson<Config>();

            Enable = _config.Enable;
            ProxyAll = _config.ProxyAll;
            TargetName = _config.TargetName;
            IP = _config.IP;
            LanIPs = _config.LanIPs;
            SocksPort = _config.SocksPort;
            BufferSize = _config.BufferSize;
            ConnectEnable = _config.ConnectEnable;


            await configDataProvider.Save(jsonStr).ConfigureAwait(false);
        }
    }
}
