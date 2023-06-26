using common.libs;
using common.libs.database;
using common.libs.extends;
using common.server;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace common.vea
{
    /// <summary>
    /// 组网配置文件
    /// </summary>
    [Table("vea-dhcp-appsettings")]
    public sealed class Config
    {
        public const byte plugin = 8;
        public const uint access = 0b00000000_00000000_00000000_01000000;
        public Config() { }
        private int lockObject = 0;
        private readonly IConfigDataProvider<Config> configDataProvider;

        public Config(IConfigDataProvider<Config> configDataProvider, WheelTimer<object> wheelTimer)
        {
            this.configDataProvider = configDataProvider;

            Config config = ReadConfig().Result;
            DHCP = config.DHCP;
            Enable = config.Enable;
            DefaultIP = config.DefaultIP;

            SaveConfig().Wait();
            AppDomain.CurrentDomain.ProcessExit += (sender, arg) => SaveConfig().Wait();
            Console.CancelKeyPress += (sender, arg) => SaveConfig().Wait();
            wheelTimer.NewTimeout(new WheelTimerTimeoutTask<object>
            {
                Callback = (timeout) =>
                {
                    if (Interlocked.CompareExchange(ref lockObject, 0, 1) == 1)
                    {
                        SaveConfig().Wait();
                    }
                }
            }, 5000, true);
        }

        [JsonIgnore]
        public byte Plugin => Config.plugin;
        [JsonIgnore]
        public uint Access => Config.access;

        public uint DefaultIPValue => _defaultIPValue;
        private uint _defaultIPValue = 0;

        public bool Enable { get; set; }

        private IPAddress _defaultIP = IPAddress.Parse("192.168.54.0");
        public IPAddress DefaultIP
        {
            get => _defaultIP; set
            {
                _defaultIP = value;
                _defaultIPValue = BinaryPrimitives.ReadUInt32BigEndian(_defaultIP.GetAddressBytes()) & 0xffffff00;
            }
        }

        public Dictionary<string, DHCPInfo> DHCP { get; set; } = new Dictionary<string, DHCPInfo>();

        /// <summary>
        /// 读取配置文件
        /// </summary>
        /// <returns></returns>
        public async Task<Config> ReadConfig()
        {
            return await configDataProvider.Load() ?? new Config();
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
            Config _config = jsonStr.DeJson<Config>();
            Enable = _config.Enable;
            DefaultIP = _config.DefaultIP;
            await configDataProvider.Save(jsonStr).ConfigureAwait(false);

        }
        /// <summary>
        /// 保存配置文件
        /// </summary>
        /// <returns></returns>
        public async Task SaveConfig()
        {
            await configDataProvider.Save(this).ConfigureAwait(false);

        }

        /// <summary>
        /// 添加网络
        /// </summary>
        /// <param name="key"></param>
        /// <param name="ip"></param>
        /// <param name="changeCallback"></param>
        /// <returns></returns>
        public DHCPInfo AddNetwork(string key, uint ip, Action<AssignedInfo> changeCallback = null)
        {
            if (DHCP.TryGetValue(key, out DHCPInfo info) == false)
            {
                info = new DHCPInfo();
                info.IP = ip & 0xffffff00;
                DHCP.Add(key, info);
                Interlocked.Exchange(ref lockObject, 1);
            }
            else
            {
                bool changed = info.IP != (ip & 0xffffff00);
                info.IP = ip & 0xffffff00;
                if (changed)
                {
                    Interlocked.Exchange(ref lockObject, 1);
                    if (changeCallback != null)
                    {
                        foreach (var item in info.Assigned)
                        {
                            item.Value.IP = info.IP | (item.Value.IP & 0xff);
                            changeCallback(item.Value);
                        }
                    }
                }
            }

            return info;
        }
        /// <summary>
        /// 获取网络
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public DHCPInfo GetNetwork(string key)
        {
            if (DHCP.TryGetValue(key, out DHCPInfo info))
            {
                return info;
            }
            return AddNetwork(key, DefaultIPValue);
        }
        /// <summary>
        /// 分配ip
        /// </summary>
        /// <param name="key"></param>
        /// <param name="ip"></param>
        /// <param name="connection"></param>
        /// <param name="name"></param>
        /// <returns>0则失败</returns>
        public uint AssignIP(string key, byte ip, IConnection connection, string name)
        {
            lock (DHCP)
            {
                DHCPInfo info = GetNetwork(key);
                byte value = ip;
                if (info.Assigned.TryGetValue(connection.ConnectId, out AssignedInfo assign) == false)
                {
                    if (info.Used.Length != 4) info.Used = new ulong[4];
                    assign = new AssignedInfo();
                    info.Assigned.Add(connection.ConnectId, assign);
                }

                //存在了，但是不是本节点的ip，就找个新的ip，找不到，就返回失败
                if(Exists(info.Used, ip) && (assign.IP & 0xff) != ip)
                {
                    if(Find(info.Used, out value) == false)
                    {
                        return 0;
                    }
                }
                Add(info.Used, value);

                assign.IP = info.IP | value;
                assign.Name = name;
                assign.Connection = connection;
                assign.LastTime = DateTime.Now;
                Interlocked.Exchange(ref lockObject, 1);
                return assign.IP;
            }
        }
        /// <summary>
        /// 存在一个不属于自己的ip
        /// </summary>
        /// <param name="key"></param>
        /// <param name="connectionId"></param>
        /// <param name="ip"></param>
        /// <returns></returns>
        public bool ExistsIP(string key, ulong connectionId, byte ip)
        {
            DHCPInfo info = GetNetwork(key);
            if (Exists(info.Used, ip))
            {
                //存在ip，但是这个连接没分配或者这个连接分配的不是这个ip，那就是被占用了，不能用
                return info.Assigned.TryGetValue(connectionId, out AssignedInfo assign) == false || (assign.IP & 0xff) != ip;
            }
            return false;
        }
        /// <summary>
        /// 更改ip，
        /// </summary>
        /// <param name="group"></param>
        /// <param name="connectionId"></param>
        /// <param name="ip"></param>
        /// <returns>0则失败</returns>
        public uint ModifyIP(string key, ulong connectionId, byte ip)
        {
            DHCPInfo info = GetNetwork(key);

            if (ExistsIP(key, connectionId, ip))
            {
                return 0;
            }

            if (info.Assigned.TryGetValue(connectionId, out AssignedInfo assign))
            {
                assign.IP = (info.IP & 0xffffff00) | ip;
                Interlocked.Exchange(ref lockObject, 1);
                return assign.IP;
            }
            return 0;
        }
        /// <summary>
        /// 删除ip
        /// </summary>
        /// <param name="key"></param>
        /// <param name="connectionId"></param>
        /// <returns></returns>
        public bool DeleteIP(string key, ulong connectionId)
        {
            if (DHCP.TryGetValue(key, out DHCPInfo info))
            {
                if (info.Assigned.Remove(connectionId, out AssignedInfo assign))
                {
                    Delete(info.Used, (byte)(assign.IP & 0xff));
                    Interlocked.Exchange(ref lockObject, 1);
                }
            }
            return true;
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
        /// 是否存在一个ip
        /// </summary>
        /// <param name="group"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private bool Exists(ulong[] array, byte value)
        {
            if (array.Length != 4) throw new Exception("array length must be 4");
            int arrayIndex = value / 64;
            int length = value - arrayIndex * 64;

            return (array[arrayIndex] >> (length - 1) & 0b1) == 1;
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
        public bool OnLine { get; set; }
        public DateTime LastTime { get; set; } = DateTime.Now;

        [JsonIgnore]
        public IConnection Connection { get; set; }
        public string Name { get; set; }
    }
}
