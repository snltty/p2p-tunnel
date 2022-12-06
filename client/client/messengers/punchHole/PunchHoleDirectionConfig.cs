using common.libs.database;
using common.libs.extends;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace client.messengers.punchHole
{
    /// <summary>
    /// 打洞方向缓存，记录下我主动连接别人成功的记录，几下谁被我主动连是成功的，下次还是由我主动
    /// </summary>
    public class PunchHoleDirectionConfig
    {
        private readonly IConfigDataProvider<PunchHoleDirectionConfig1> configDataProvider;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="configDataProvider"></param>
        public PunchHoleDirectionConfig(IConfigDataProvider<PunchHoleDirectionConfig1> configDataProvider)
        {
            this.configDataProvider = configDataProvider;

            config = ReadConfig().Result ?? new PunchHoleDirectionConfig1();
        }

        PunchHoleDirectionConfig1 config { get; set; }

        /// <summary>
        /// 添加一条
        /// </summary>
        /// <param name="name"></param>
        public void Add(string name)
        {
            config.Names.Add(name);
            config.Names = config.Names.Distinct().ToList();
            _ = SaveConfig(config.ToJson());
        }
        /// <summary>
        /// 是否包含
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool Contains(string name)
        {
            return config.Names.Contains(name);
        }

        /// <summary>
        /// 读取
        /// </summary>
        /// <returns></returns>
        public async Task<string> ReadString()
        {
            return await configDataProvider.LoadString();
        }

        private async Task<PunchHoleDirectionConfig1> ReadConfig()
        {
            return await configDataProvider.Load();
        }
        private async Task SaveConfig(string jsonStr)
        {
            await configDataProvider.Save(jsonStr).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 打洞方向缓存配置文件
    /// </summary>
    [Table("punchhole-direction")]
    public class PunchHoleDirectionConfig1
    {
        /// <summary>
        /// 客户端名字列表
        /// </summary>
        public List<string> Names { get; set; } = new List<string>();
        /// <summary>
        /// 
        /// </summary>
        public PunchHoleDirectionConfig1() { }
    }
}
