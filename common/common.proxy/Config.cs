using common.libs;
using common.libs.database;
using common.libs.extends;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace common.proxy
{
    [Table("proxy-appsettings")]
    public sealed class Config
    {
        public Config() { }
        private readonly IConfigDataProvider<Config> configDataProvider;
        NumberSpaceUInt32 ids = new NumberSpaceUInt32(0);

        public Config(IConfigDataProvider<Config> configDataProvider)
        {
            this.configDataProvider = configDataProvider;

            Config config = ReadConfig().Result;
            Firewall = config.Firewall;
            ParseFirewall();
            ids.Reset(Firewall.Count > 0 ? Firewall.Max(c => c.ID) : 0);
        }

        public List<FirewallItem> Firewall { get; set; } = new List<FirewallItem>();
        [JsonIgnore]
        public Dictionary<uint, FirewallCache> AllowFirewalls { get; set; } = new Dictionary<uint, FirewallCache>();
        [JsonIgnore]
        public Dictionary<uint, FirewallCache> DeniedFirewalls { get; set; } = new Dictionary<uint, FirewallCache>();


        public async Task<bool> AddFirewall(FirewallItem model)
        {
            FirewallItem item = Firewall.FirstOrDefault(c => c.ID == model.ID) ?? new FirewallItem { };
            FirewallItem old = Firewall.FirstOrDefault(c => c.Port == model.Port && c.Protocol == model.Protocol && c.Type == model.Type && c.PluginId == model.PluginId);
            if (old != null && old.ID != model.ID)
            {
                return false;
            }

            if (item.ID > 0)
            {
                item.Type = model.Type;
                item.Protocol = model.Protocol;
                item.PluginId = model.PluginId;
                item.Port = model.Port;
                item.Remark = model.Remark;
                item.IP = model.IP;
            }
            else
            {
                model.ID = ids.Increment();
                Firewall.Add(model);
            }
            ParseFirewall();
            await SaveConfig();
            return true;
        }
        public async Task<bool> RemoveFirewall(uint id)
        {
            Firewall.Remove(Firewall.FirstOrDefault(c => c.ID == id));
            await SaveConfig();
            return true;
        }

        private void ParseFirewall()
        {
            AllowFirewalls.Clear();
            DeniedFirewalls.Clear();
            foreach (FirewallItem item in Firewall)
            {
                try
                {
                    ushort[] ports = PrarsePort(item.Port);
                    FirewallCacheIp[] ips = ParseIp(item.IP);
                    FirewallCache cache = new FirewallCache
                    {
                        IPs = ips
                    };
                    for (int i = 0; i < ports.Length; i++)
                    {
                        if (item.Type == FirewallType.Allow)
                        {
                            if ((item.Protocol & FirewallProtocolType.TCP) == FirewallProtocolType.TCP)
                                AllowFirewalls[new FirewallKey(ports[i], FirewallProtocolType.TCP, item.PluginId).Memory] = cache;
                            if ((item.Protocol & FirewallProtocolType.UDP) == FirewallProtocolType.UDP)
                                AllowFirewalls[new FirewallKey(ports[i], FirewallProtocolType.UDP, item.PluginId).Memory] = cache;
                        }
                        else
                        {
                            if ((item.Protocol & FirewallProtocolType.TCP) == FirewallProtocolType.TCP)
                                DeniedFirewalls[new FirewallKey(ports[i], FirewallProtocolType.TCP, item.PluginId).Memory] = cache;
                            if ((item.Protocol & FirewallProtocolType.UDP) == FirewallProtocolType.UDP)
                                DeniedFirewalls[new FirewallKey(ports[i], FirewallProtocolType.UDP, item.PluginId).Memory] = cache;
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
        }
        private ushort[] PrarsePort(string port)
        {
            if (string.IsNullOrWhiteSpace(port)) return Array.Empty<ushort>();

            return port.Split(Helper.SeparatorCharComma).SelectMany(c =>
            {
                string[] arr = c.Split(Helper.SeparatorCharSlash);
                if (arr.Length == 1)
                {
                    return new ushort[1] { ushort.Parse(arr[0]) };
                }
                ushort start = ushort.Parse(arr[0]);
                ushort end = ushort.Parse(arr[1]);
                if (start == 1 && end == 65535) return new ushort[1] { 0 };

                return Helper.Range(start, end);

            }).ToArray();
        }
        private FirewallCacheIp[] ParseIp(string[] ips)
        {
            return ips.Select(c => c.Split(Helper.SeparatorCharSlash)).Select(c =>
            {
                byte maskLength = c.Length > 1 ? byte.Parse(c[1]) : (byte)0;
                uint ip = BinaryPrimitives.ReadUInt32BigEndian(IPAddress.Parse(c[0]).GetAddressBytes());
                //没填写掩码，自动计算
                if (c.Length == 1)
                {
                    maskLength = NetworkHelper.MaskLength(ip);
                }
                //掩码十进制
                uint maskValue = NetworkHelper.MaskValue(maskLength);
                return new FirewallCacheIp
                {
                    MaskValue = maskValue,
                    //网络号
                    NetWork = ip & maskValue
                };
            }).ToArray();
        }

        public async Task<Config> ReadConfig()
        {
            Config config = await configDataProvider.Load();
            return config;
        }
        public async Task<string> ReadString()
        {
            return await configDataProvider.LoadString();
        }
        public async Task SaveConfig(string jsonStr)
        {
            Config config = jsonStr.DeJson<Config>();
            Firewall = config.Firewall;
            ParseFirewall();
            await configDataProvider.Save(jsonStr).ConfigureAwait(false);
        }
        public async Task SaveConfig()
        {
            ParseFirewall();
            await configDataProvider.Save(this).ConfigureAwait(false);
        }
    }

    public sealed class FirewallCache
    {
        public FirewallCacheIp[] IPs { get; set; } = Array.Empty<FirewallCacheIp>();
    }
    public sealed class FirewallCacheIp
    {
        public uint MaskValue { get; set; }
        public uint NetWork { get; set; }
    }

    public sealed class FirewallItem
    {
        public uint ID { get; set; }
        public byte PluginId { get; set; }
        public FirewallProtocolType Protocol { get; set; }
        public FirewallType Type { get; set; }
        public string Port { get; set; } = string.Empty;
        public string[] IP { get; set; } = Array.Empty<string>();
        public string Remark { get; set; } = string.Empty;
    }

    public enum FirewallProtocolType : byte
    {
        TCP = 1,
        UDP = 2,
        TCP_UDP = TCP | UDP,
    }
    public enum FirewallType : byte
    {
        Allow = 0,
        Denied = 1,
    }


    [StructLayout(LayoutKind.Explicit)]
    public readonly struct FirewallKey
    {
        [FieldOffset(0)]
        public readonly uint Memory;

        [FieldOffset(0)]
        public readonly ushort Port;

        [FieldOffset(2)]
        public readonly FirewallProtocolType Protocol;

        [FieldOffset(3)]
        public readonly byte PluginId;

        public FirewallKey(ushort port, FirewallProtocolType protocol, byte pluginId)
        {
            Port = port;
            Protocol = protocol;
            PluginId = pluginId;
        }
    }
}
