using common.libs.database;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace client.messengers.punchHole
{

    public class PunchHoleDirectionConfig
    {
        private readonly IConfigDataProvider<PunchHoleDirectionConfig1> configDataProvider;

        public PunchHoleDirectionConfig(IConfigDataProvider<PunchHoleDirectionConfig1> configDataProvider)
        {
            this.configDataProvider = configDataProvider;

            config = ReadConfig().Result ?? new PunchHoleDirectionConfig1();
        }

        PunchHoleDirectionConfig1 config { get; set; }

        public void Add(string name)
        {
            config.Names.Add(name);
            config.Names = config.Names.Distinct().ToList();
            _ = SaveConfig();
        }
        public bool Contains(string name)
        {
            return config.Names.Contains(name);
        }

        private async Task<PunchHoleDirectionConfig1> ReadConfig()
        {
            return await configDataProvider.Load();
        }
        private async Task SaveConfig()
        {
            PunchHoleDirectionConfig1 _config = await ReadConfig().ConfigureAwait(false);
            _config.Names = config.Names;
            await configDataProvider.Save(_config).ConfigureAwait(false);
        }
    }

    [Table("punchhole-direction")]
    public class PunchHoleDirectionConfig1
    {
        public List<string> Names { get; set; } = new List<string>();
        public PunchHoleDirectionConfig1() { }
    }
}
