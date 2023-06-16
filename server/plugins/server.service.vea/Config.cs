using common.libs.database;
using common.libs.extends;
using server.messengers.singnin;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace server.service.vea
{
    /// <summary>
    /// 组网配置文件
    /// </summary>
    [Table("vea-dhcp")]
    public sealed class Config
    {
        public Config() { }
        private readonly IConfigDataProvider<Config> configDataProvider;


        public Config(IConfigDataProvider<Config> configDataProvider)
        {
            this.configDataProvider = configDataProvider;

            Config config = ReadConfig().Result;
            DHCP = config.DHCP;
        }

        object lockObj = new object();
        public Dictionary<string, DHCPInfo> DHCP { get; set; } = new Dictionary<string, DHCPInfo>();

        public DHCPInfo Add(string group, uint ip)
        {
            if (DHCP.TryGetValue(group, out DHCPInfo info) == false)
            {
                info = new DHCPInfo();
                DHCP.Add(group, info);
            }
            info.Start = ip & 0xffffff00 | 1;
            info.End = ip & 0xffffff00 | 254;
            return info;
        }
        public DHCPInfo Get(string group, uint ip)
        {
            if (DHCP.TryGetValue(group, out DHCPInfo info))
            {
                return info;
            }
            return Add(group, ip);
        }
        public uint Assign(SignInCacheInfo sign, uint ip)
        {
            lock (lockObj)
            {
                DHCPInfo info = Get(sign.GroupId, ip);
                if (info.Assigned.TryGetValue(sign.ConnectionId, out AssignedInfo assign) == false)
                {
                    uint _ip = 0;
                    if (info.Assigned.Count == 0)
                    {
                        _ip = info.Start;
                    }
                    else
                    {
                        //每一段
                        for (uint i = 0; i < info.Used.Length; i++)
                        {
                            ulong item = info.Used[i];
                            //这一段满了
                            if (item >= ulong.MaxValue)
                            {
                                continue;
                            }
                            //每个字节
                            for (int j = 0; j < 8; j++)
                            {
                                byte b = (byte)((item >> j) & 0b11111111);
                                //这字节
                                if (b >= byte.MaxValue)
                                {
                                    continue;
                                }
                                //每一位
                                for (int k = 0; k < 8; k++)
                                {
                                    if (((b >> k) & 0b1) == 0)
                                    {
                                        //找到了可用的ip
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    if (_ip == 0) return 0;
                    assign = new AssignedInfo { IP = _ip, Name = sign.Name };
                    info.Assigned.Add(sign.ConnectionId, assign);
                }
                return assign.IP;
            }
        }


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
            DHCP = _config.DHCP;
            await configDataProvider.Save(jsonStr).ConfigureAwait(false);

        }

    }

    public sealed class DHCPInfo
    {
        public uint Start { get; set; }
        public uint End { get; set; }
        public Dictionary<ulong, AssignedInfo> Assigned { get; set; } = new Dictionary<ulong, AssignedInfo>();
        public ulong[] Used { get; set; } = new ulong[8] { 0, 0, 0, 0, 0, 0, 0, 0 };
    }
    public sealed class AssignedInfo
    {
        public uint IP { get; set; }
        public string Name { get; set; }
    }
}
