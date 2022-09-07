using common.libs.database;
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

        public async Task SaveConfig()
        {
            Config config = await ReadConfig().ConfigureAwait(false);
            config.Enable = Enable;
            config.MaxLength = MaxLength;

            await configDataProvider.Save(config).ConfigureAwait(false);
        }
    }
}
