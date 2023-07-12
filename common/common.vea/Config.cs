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
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);

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
                info = new DHCPInfo
                {
                    IP = ip & 0xffffff00
                };
                DHCP.Add(key, info);
                info.Parse();
                Interlocked.Exchange(ref lockObject, 1);
            }
            else
            {
                bool changed = info.IP != (ip & 0xffffff00);
                info.IP = ip & 0xffffff00;
                if (changed)
                {
                    foreach (var item in info.Assigned)
                    {
                        item.Value.IP = info.IP | (item.Value.IP & 0xff);
                        changeCallback?.Invoke(item.Value);
                    }
                    Interlocked.Exchange(ref lockObject, 1);
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
            semaphore.Wait();
            try
            {
                DHCPInfo info = GetNetwork(key);
                info.Parse();

                if (info.Assigned.TryGetValue(connection.ConnectId, out AssignedInfo assign) == false)
                {
                    assign = new AssignedInfo();
                    Interlocked.Exchange(ref lockObject, 1);

                    //存在了，就找个新的ip，找不到，就返回失败
                    if (info.Exists(ip) && info.Find(out ip) == false)
                    {
                        return 0;
                    }
                    info.Assigned.Add(connection.ConnectId, assign);
                    info.Add(ip);
                    assign.IP = info.IP | ip;
                }

                assign.Name = name;
                assign.Connection = connection;
                assign.LastTime = DateTime.Now;
                Interlocked.Exchange(ref lockObject, 1);
                return assign.IP;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);  
            }
            finally
            {
                semaphore.Release();
            }
            return 0;
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
            if (info.Exists(ip))
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
                info.Delete((byte)(assign.IP & 0xff));
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
                    info.Delete((byte)(assign.IP & 0xff));
                    Interlocked.Exchange(ref lockObject, 1);
                }
            }
            return true;
        }

    }

    public sealed class DHCPInfo
    {
        public uint IP { get; set; }
        public Dictionary<ulong, AssignedInfo> Assigned { get; set; } = new Dictionary<ulong, AssignedInfo>();
        public ulong[] Used { get; set; } = new ulong[4] { 0, 0, 0, 0 };
        public void Parse()
        {
            Used[0] = 0;
            Used[1] = 0;
            Used[2] = 0;
            Used[3] = 0;
            Add(1);
            Add(254);
            Add(255);
            foreach (var item in Assigned.Values)
            {
                Add((byte)(item.IP & 0xff));
            }
        }
        public void Add(byte value)
        {
            int arrayIndex = value / 64;
            int length = value - arrayIndex * 64;
            Used[arrayIndex] |= (ulong)1 << (length - 1);
        }
        public bool Exists(byte value)
        {
            int arrayIndex = value / 64;
            int length = value - arrayIndex * 64;

            return (Used[arrayIndex] >> (length - 1) & 0b1) == 1;
        }
        public void Delete(byte value)
        {
            int arrayIndex = value / 64;
            int length = value - arrayIndex * 64;
            Used[arrayIndex] &= ~((ulong)1 << (length - 1));
        }

        public bool Find(out byte value)
        {
            value = 0;
            if (Used.Length != 4) throw new Exception("array length must be 4");

            if (Used[0] < ulong.MaxValue) value = Find(Used[0], 0);
            else if (Used[1] < ulong.MaxValue) value = Find(Used[1], 1);
            else if (Used[2] < ulong.MaxValue) value = Find(Used[2], 2);
            else if (Used[3] < ulong.MaxValue) value = Find(Used[3], 3);
            return value > 0;
        }
        public byte Find(ulong group, byte index)
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
