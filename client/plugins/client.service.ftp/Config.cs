using common.libs.database;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Threading.Tasks;

namespace client.service.ftp
{
    [Table("ftp-appsettings")]
    public class Config
    {
        private readonly IConfigDataProvider<Config> configDataProvider;

        public Config() { }

        public Config(IConfigDataProvider<Config> configDataProvider)
        {
            this.configDataProvider = configDataProvider;

            Config config = ReadConfig().Result;
            ServerRoot = config.ServerRoot;
            Password = config.Password;
            Enable = config.Enable;
            UploadNum = config.UploadNum;
            ReadWriteBufferSize = config.ReadWriteBufferSize;
            SendPacketSize = config.SendPacketSize;
        }

        private string serverRoot = string.Empty;
        public string ServerRoot
        {
            get
            {
                return serverRoot;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    serverRoot = Directory.GetCurrentDirectory();
                }
                else
                {
                    serverRoot = new DirectoryInfo(value).FullName;
                }
            }
        }

        [System.Text.Json.Serialization.JsonIgnore]
        public string ServerCurrentPath { get; set; } = string.Empty;
        [System.Text.Json.Serialization.JsonIgnore]
        public string ClientRootPath { get; set; } = string.Empty;
        [System.Text.Json.Serialization.JsonIgnore]
        public string ClientCurrentPath { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
        public bool Enable { get; set; } = false;
        public int UploadNum { get; set; } = 10;
        public int ReadWriteBufferSize { get; set; } = 10 * 1024 * 1024;
        public int SendPacketSize { get; set; } = 32 * 1024;


        public async Task<Config> ReadConfig()
        {
            return await configDataProvider.Load().ConfigureAwait(false);
        }

        public async Task SaveConfig()
        {
            Config config = await ReadConfig().ConfigureAwait(false);

            config.ServerRoot = ServerRoot;
            config.Password = Password;
            config.Enable = Enable;
            config.UploadNum = UploadNum;
            config.ReadWriteBufferSize = ReadWriteBufferSize;
            config.SendPacketSize = SendPacketSize;

            await configDataProvider.Save(config).ConfigureAwait(false);
        }
    }
}
