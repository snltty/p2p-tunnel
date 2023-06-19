using common.libs.database;
using common.libs.extends;
using server.messengers.singnin;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
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

        public DHCPInfo AddNetwork(string group, uint ip)
        {
            if (DHCP.TryGetValue(group, out DHCPInfo info) == false)
            {
                info = new DHCPInfo();
                DHCP.Add(group, info);
            }
            info.IP = ip & 0xffffff00;
            SaveConfig().Wait();
            return info;
        }
        public DHCPInfo GetNetwork(string group, uint ip)
        {
            if (DHCP.TryGetValue(group, out DHCPInfo info))
            {
                return info;
            }
            return AddNetwork(group, ip);
        }
        public uint AssignIP(SignInCacheInfo sign, uint ip)
        {
            lock (lockObj)
            {
                DHCPInfo info = GetNetwork(sign.GroupId, ip);
                if (info.Assigned.TryGetValue(sign.ConnectionId, out AssignedInfo assign) == false)
                {
                    if (info.Used.Length != 4) info.Used = new ulong[4];
                    if (Find(info.Used, out byte value) == false)
                    {
                        return 0;
                    }
                    Add(info.Used, value);
                    assign = new AssignedInfo { IP = info.IP | value, Name = sign.Name };
                    info.Assigned.Add(sign.ConnectionId, assign);
                    SaveConfig().Wait();
                }
                return assign.IP;
            }
        }
        public bool DeleteIP(string group, ulong connectionId)
        {
            if (DHCP.TryGetValue(group, out DHCPInfo info))
            {
                if (info.Assigned.Remove(connectionId, out AssignedInfo assign))
                {
                    Delete(info.Used, (byte)(assign.IP & 0b11111111));
                }
            }
            return true;
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
        public async Task SaveConfig()
        {
            await configDataProvider.Save(this).ConfigureAwait(false);

        }

        /// <summary>
        /// 查找网段内可用ip(/24)
        /// </summary>
        /// <param name="array">缓存数组</param>
        /// <param name="value">找到的值</param>
        /// <returns></returns>
        /// <exception cref="Exception">array length must be 4</exception>
        private bool Find(ulong[] array, out byte value)
        {
            value = 0;
            if (array.Length != 4) throw new Exception("array length must be 4");
            //排除 .1 .255 .256
            array[0] |= 0b1;
            array[3] |= (ulong)0b11 << 62;

            if (array[0] < ulong.MaxValue) value = Find(array[0], 0);
            else if (array[1] < ulong.MaxValue) value = Find(array[1], 1);
            else if (array[2] < ulong.MaxValue) value = Find(array[2], 2);
            else if (array[3] < ulong.MaxValue) value = Find(array[3], 3);
            return value > 0;
        }
        private byte Find(ulong group, byte index)
        {
            byte value = (byte)(index * 64);
            //每次对半开，也可以循环，循环稍微会慢3-4ns，常量值快一点
            ulong _group = (group & uint.MaxValue);
            if (_group >= uint.MaxValue) { _group = group >> 32; value += 32; }
            group = _group;

            _group = (group & ushort.MaxValue);
            if (_group >= ushort.MaxValue) { _group = group >> 16; value += 16; }
            group = _group;

            _group = (group & byte.MaxValue);
            if (_group >= byte.MaxValue) { _group = group >> 8; value += 8; }
            group = _group;

            _group = (group & 0b1111);
            if (_group >= 0b1111) { _group = group >> 4; value += 4; }
            group = _group;

            _group = (group & 0b11);
            if (_group >= 0b11) { _group = group >> 2; value += 2; }
            group = _group;

            _group = (group & 0b1);
            if (_group >= 0b1) { value += 1; }
            value += 1;

            return value;
        }
        /// <summary>
        /// 将一个ip(/24)设为已使用
        /// </summary>
        /// <param name="array">缓存数组</param>
        /// <param name="value">值</param>
        private void Add(ulong[] array, byte value)
        {
            if (array.Length != 4) throw new Exception("array length must be 4");
            int arrayIndex = value / 64;
            int length = value - arrayIndex * 64;
            array[arrayIndex] |= (ulong)1 << (length - 1);
        }
        /// <summary>
        /// 删除一个ip(/24),设为未使用
        /// </summary>
        /// <param name="array"></param>
        /// <param name="value"></param>
        /// <exception cref="Exception"></exception>
        private void Delete(ulong[] array, byte value)
        {
            if (array.Length != 4) throw new Exception("array length must be 4");
            int arrayIndex = value / 64;
            int length = value - arrayIndex * 64;
            array[arrayIndex] &= ~((ulong)1 << (length - 1));
        }

    }

    public sealed class DHCPInfo
    {
        public uint IP { get; set; }
        public Dictionary<ulong, AssignedInfo> Assigned { get; set; } = new Dictionary<ulong, AssignedInfo>();
        public ulong[] Used { get; set; } = new ulong[4] { 0, 0, 0, 0 };
    }
    public sealed class AssignedInfo
    {
        public uint IP { get; set; }
        public string Name { get; set; }
    }
}
