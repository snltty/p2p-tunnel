using common.libs.database;
using common.libs.extends;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;

namespace client.service.logger
{
    [Table("logger-appsettings")]
    public class Config
    {
        public Config() { }
        private readonly IConfigDataProvider<Config> configDataProvider;
        public Config(IConfigDataProvider<Config> configDataProvider)
        {
            this.configDataProvider = configDataProvider;

            Config config = ReadConfig().Result;
            Enable = config.Enable;
            MaxLength = config.MaxLength;
        }


        public bool Enable { get; set; } = false;
        public int MaxLength { get; set; } = 100;

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
            Config _config = jsonStr.DeJson<Config>();

            Enable = _config.Enable;
            MaxLength = _config.MaxLength;
            await configDataProvider.Save(jsonStr).ConfigureAwait(false);
        }
    }
}
