using common.libs.database;
using common.libs.extends;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;
using System.Threading.Tasks;

namespace client.service.vea
{
    [Table("vea-appsettings")]
    public class Config
    {
        public Config() { }
        private readonly IConfigDataProvider<Config> configDataProvider;
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


        public bool Enable { get; set; } = false;
        public bool ProxyAll { get; set; } = false;
        public string TargetName { get; set; } = string.Empty;
        public IPAddress IP { get; set; } = IPAddress.Parse("192.168.54.1");
        public IPAddress[] LanIPs { get; set; } = Array.Empty<IPAddress>();

        public int SocksPort { get; set; } = 5415;
        public int BufferSize { get; set; } = 8 * 1024;
        public int NumConnections { get; set; } = 1000;
        public bool ConnectEnable { get; set; } = false;

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

            Enable = _config.Enable;
            ProxyAll = _config.ProxyAll;
            TargetName = _config.TargetName;
            IP = _config.IP;
            SocksPort = _config.SocksPort;
            BufferSize = _config.BufferSize;
            ConnectEnable = _config.ConnectEnable;


            await configDataProvider.Save(jsonStr).ConfigureAwait(false);
        }
    }
}
