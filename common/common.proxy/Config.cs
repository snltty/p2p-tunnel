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
            HttpHeader = config.HttpHeader;
            ParseFirewall();
            ids.Reset(Firewall.Count > 0 ? Firewall.Max(c => c.ID) : 0);
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
            HttpHeader = config.HttpHeader;
            ParseFirewall();
            await configDataProvider.Save(jsonStr).ConfigureAwait(false);
        }
        public async Task SaveConfig()
        {
            ParseFirewall();
            await configDataProvider.Save(this).ConfigureAwait(false);
        }


        public List<FirewallItem> Firewall { get; set; } = new List<FirewallItem>();
        [JsonIgnore]
        public FirewallCacheType[] Firewall0 { get; } = new FirewallCacheType[2];
        [JsonIgnore]
        public Dictionary<ushort, FirewallCacheType[]> Firewalls { get; } = new Dictionary<ushort, FirewallCacheType[]>();
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
            Array.Clear(Firewall0);
            Firewalls.Clear();
            foreach (FirewallItem item in Firewall)
            {
                try
                {
                    ushort[] ports = PrarsePort(item.Port);
                    ulong[] ips = ParseIp(item.IP);
                    for (int i = 0; i < ports.Length; i++)
                    {
                        FirewallCacheType[] types = Firewall0;
                        if (ports[i] != 0)
                        {
                            if (Firewalls.TryGetValue(ports[i], out types) == false)
                            {
                                types = new FirewallCacheType[2];
                                Firewalls.Add(ports[i], types);
                            }
                        }

                        FirewallCacheType type = types[(byte)item.Type];
                        if (type == null)
                        {
                            type = new FirewallCacheType();
                            types[(byte)item.Type] = type;
                        }

                        if (item.PluginId == 0) item.PluginId = 0xff;
                        type.PluginIds |= item.PluginId;
                        type.Protocols |= item.Protocol;
                        type.Ips = type.Ips.Concat(ips).Distinct().ToArray();
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
        private ulong[] ParseIp(string[] ips)
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
                return new FirewallCacheIp(maskValue, ip & maskValue).Value;
            }).ToArray();
        }

        public List<HttpHeaderItem> HttpHeader { get; set; } = new List<HttpHeaderItem>();
        [JsonIgnore]
        public FirewallCacheType[] HttpHeader0 { get; } = new FirewallCacheType[2];
        [JsonIgnore]
        public Dictionary<ushort, FirewallCacheType[]> HttpHeaders { get; } = new Dictionary<ushort, FirewallCacheType[]>();

        public async Task SetHeaders(List<HttpHeaderItem> headers)
        {
            HttpHeader = headers;
            await SaveConfig();
        }
    }


    public sealed class FirewallCacheType
    {
        public FirewallProtocolType Protocols { get; set; }
        public byte PluginIds { get; set; }
        public ulong[] Ips { get; set; } = Array.Empty<ulong>();
    }
    [StructLayout(LayoutKind.Explicit)]
    public struct FirewallCacheIp
    {
        [FieldOffset(0)]
        public ulong Value;
        [FieldOffset(0)]
        public uint MaskValue;
        [FieldOffset(4)]
        public uint NetWork;
        public FirewallCacheIp(ulong value)
        {
            Value = value;
        }
        public FirewallCacheIp(uint maskValue, uint netWork)
        {
            MaskValue = maskValue;
            NetWork = netWork;
        }
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



    public sealed class HttpHeaderItem
    {
        public byte PluginId { get; set; }
        public HttpHeaderDynamicType Dynamics { get; set; }
        public Dictionary<string, string> Statics { get; set; } = new Dictionary<string, string>();
    }
    public enum HttpHeaderDynamicType:byte
    {
        Addr = 1,
        Name = 2,
        Account = 4
    }
}
