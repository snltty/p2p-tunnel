using client.messengers.clients;
using client.messengers.singnin;
using common.libs;
using common.libs.extends;
using common.proxy;
using common.server;
using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace client.service.vea
{
    /// <summary>
    /// 组网
    /// </summary>
    public sealed class VeaTransfer
    {
        Process Tun2SocksProcess;
        int interfaceNumber = 0;
        string interfaceLinux = string.Empty;
        string interfaceOsx = string.Empty;
        const string veaName = "p2p-tunnel";
        const string veaNameOsx = "utun12138";

        private readonly ConcurrentDictionary<uint, IPAddressCacheInfo> ips = new ConcurrentDictionary<uint, IPAddressCacheInfo>();
        private readonly ConcurrentDictionary<uint, IPAddressCacheInfo> lanips = new ConcurrentDictionary<uint, IPAddressCacheInfo>();
        public ConcurrentDictionary<uint, IPAddressCacheInfo> IPList => ips;
        public ConcurrentDictionary<uint, IPAddressCacheInfo> LanIPList => lanips;

        private readonly Config config;
        private readonly IClientInfoCaching clientInfoCaching;
        private readonly VeaMessengerSender veaMessengerSender;
        private readonly SignInStateInfo signInStateInfo;
        private readonly IProxyServer proxyServer;

        public VeaTransfer(Config config, IClientInfoCaching clientInfoCaching, VeaMessengerSender veaMessengerSender, SignInStateInfo signInStateInfo, IProxyServer proxyServer)
        {
            this.config = config;
            this.clientInfoCaching = clientInfoCaching;
            this.veaMessengerSender = veaMessengerSender;
            this.signInStateInfo = signInStateInfo;
            this.proxyServer = proxyServer;

            clientInfoCaching.OnOnline += (client) =>
            {
                UpdateIp();
            };
            clientInfoCaching.OnOffline += (client) =>
            {
                var value = ips.FirstOrDefault(c => c.Value.Client.Id == client.Id);
                if (value.Key != 0)
                {
                    if (ips.TryRemove(value.Key, out IPAddressCacheInfo cache))
                    {
                        RemoveLanMasks(cache.LanIPs);
                    }
                }
            };

            AppDomain.CurrentDomain.ProcessExit += (object sender, EventArgs e) => Stop();
        }

        /// <summary>
        /// 收到通知
        /// </summary>
        /// <param name="connection"></param>
        public void OnNotify(IConnection connection)
        {
            if (connection.FromConnection != null)
            {
                bool res = clientInfoCaching.Get(connection.FromConnection.ConnectId, out ClientInfo client);
                if (res)
                {
                    IPAddressInfo ips = new IPAddressInfo();
                    ips.DeBytes(connection.ReceiveRequestWrap.Payload);
                    UpdateIp(client, ips);
                }
            }

        }
        private void UpdateIp(ClientInfo client, IPAddressInfo _ips)
        {
            if (client == null || _ips == null) return;

            lock (this)
            {
                ResetMask(_ips);
                var cache = ips.Values.FirstOrDefault(c => c.Client.Id == client.Id);
                if (cache != null)
                {
                    ips.TryRemove(cache.IP, out _);
                    RemoveLanMasks(cache.LanIPs);
                }

                cache = new IPAddressCacheInfo { Client = client, IP = _ips.IP, LanIPs = _ips.LanIPs, Mask = 32 };
                ips.AddOrUpdate(_ips.IP, cache, (a, b) => cache);
                AddLanMasks(_ips, client);

                AddRoute();
            }
        }
        /// <summary>
        /// 更新ip
        /// </summary>
        public void UpdateIp()
        {
            foreach (var item in clientInfoCaching.All().Where(c => c.Id != signInStateInfo.ConnectId))
            {
                var connection = item.Connection;
                var client = item;
                if (connection != null)
                {
                    veaMessengerSender.IP(connection).ContinueWith((result) =>
                    {
                        UpdateIp(client, result.Result);
                    });
                }
            }
        }

        /// <summary>
        /// 开启
        /// </summary>
        public void Run()
        {
            Stop();
            if (config.ListenEnable)
            {
                RunTun2Socks();
                proxyServer.Start((ushort)config.ListenPort, config.Plugin); ;
            }
            UpdateIp();
        }
        /// <summary>
        /// 停止
        /// </summary>
        public void Stop()
        {
            proxyServer.Stop(config.Plugin);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                KillWindows();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                KillLinux();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                KillOsx();
            }
        }

        private void RunTun2Socks()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                WindowsIdentity id = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(id);
                if (principal.IsInRole(WindowsBuiltInRole.Administrator))
                {
                    RunWindows();
                }
                else
                {
                    throw new Exception($"需要管理员权限");
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                RunLinux();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                RunOsx();
            }
        }

        private void KillWindows()
        {
            interfaceNumber = 0;
            if (Tun2SocksProcess != null)
            {
                Tun2SocksProcess.Kill();
                Tun2SocksProcess.Close();
                Tun2SocksProcess.Dispose();
                Tun2SocksProcess = null;
            }
            foreach (var item in Process.GetProcesses().Where(c => c.ProcessName.Contains("tun2socks")))
            {
                item.Kill();
                item.Close();
                item.Dispose();
            };
        }
        private void KillLinux()
        {
            if (Tun2SocksProcess != null)
            {
                Tun2SocksProcess.Kill();
                Tun2SocksProcess.Close();
                Tun2SocksProcess.Dispose();
                Tun2SocksProcess = null;
            }

            Command.Linux(string.Empty, new string[] { $"ip tuntap del mode tun dev {veaName}" });
        }
        private void KillOsx()
        {
            if (Tun2SocksProcess != null)
            {
                Tun2SocksProcess.Kill();
                Tun2SocksProcess.Close();
                Tun2SocksProcess.Dispose();
                Tun2SocksProcess = null;
            }
            var ip = config.IP.GetAddressBytes();
            ip[^1] = 0;
            Command.Osx(string.Empty, new string[] { $"route delete -net {new IPAddress(ip)}/24 {config.IP}" });
        }

        private void RunWindows()
        {
            //return;
            for (int i = 0; i < 10; i++)
            {
                Tun2SocksProcess = Command.Execute("tun2socks-windows.exe", $" -device {veaName} -proxy socks5://127.0.0.1:{config.ListenPort} -loglevel silent");
                for (int k = 0; k < 4; k++)
                {
                    System.Threading.Thread.Sleep(1000);
                    if (GetWindowsHasInterface(veaName))
                    {
                        interfaceNumber = GetWindowsInterfaceNum();
                        if (interfaceNumber > 0)
                        {
                            Command.Windows(string.Empty, new string[] { $"netsh interface ip set address name=\"{veaName}\" source=static addr={config.IP} mask=255.255.255.0 gateway=none" });
                            System.Threading.Thread.Sleep(100);
                            if (GetWindowsHasIp(config.IP))
                            {
                                AddRoute();
                                if (config.ProxyAll) //代理所有
                                {
                                    //AddRoute(IPAddress.Any);
                                }
                                return;
                            }
                        }
                    }
                }
                KillWindows();
            }
        }
        private int GetWindowsInterfaceNum()
        {
            string output = Command.Windows(string.Empty, new string[] { "route print" });
            foreach (var item in output.Split(Environment.NewLine))
            {
                if (item.Contains("WireGuard Tunnel"))
                {
                    return int.Parse(item.Substring(0, item.IndexOf('.')).Trim());
                }
            }
            return 0;
        }
        private bool GetWindowsHasInterface(string name)
        {
            string output = Command.Windows(string.Empty, new string[] { $"ipconfig | findstr \"{name}\"" });
            return string.IsNullOrWhiteSpace(output) == false;
        }
        private bool GetWindowsHasIp(IPAddress ip)
        {
            string output = Command.Windows(string.Empty, new string[] { $"ipconfig | findstr \"{ip}\"" });
            return string.IsNullOrWhiteSpace(output) == false;
        }
        private void RunLinux()
        {
            Command.Linux(string.Empty, new string[] {
                $"ip tuntap add mode tun dev {veaName}",
                $"ip addr add {config.IP}/24 dev {veaName}",
                $"ip link set dev {veaName} up"
            });
            interfaceLinux = GetLinuxInterfaceNum();
            Tun2SocksProcess = Command.Execute("./tun2socks-linux", $" -device {veaName} -proxy socks5://127.0.0.1:{config.ListenPort} -interface {interfaceLinux} -loglevel silent");

            AddRoute();
        }
        private string GetLinuxInterfaceNum()
        {
            string output = Command.Linux(string.Empty, new string[] { "ip route" });
            foreach (var item in output.Split(Environment.NewLine))
            {
                if (item.StartsWith("default via"))
                {
                    var strs = item.Split(' ');
                    for (int i = 0; i < strs.Length; i++)
                    {
                        if (strs[i] == "dev")
                        {
                            return strs[i + 1];
                        }
                    }
                }
            }
            return string.Empty;
        }
        private void RunOsx()
        {
            interfaceOsx = GetOsxInterfaceNum();
            Tun2SocksProcess = Command.Execute("./tun2socks-osx", $" -device {veaNameOsx} -proxy socks5://127.0.0.1:{config.ListenPort} -interface {interfaceOsx} -loglevel silent");

            for (int i = 0; i < 60; i++)
            {
                string output = Command.Osx(string.Empty, new string[] { "ifconfig" });
                if (output.Contains(veaNameOsx))
                {
                    break;
                }
                else
                {
                    System.Threading.Thread.Sleep(1000);
                }
            }

            Command.Osx(string.Empty, new string[] { $"ifconfig {veaNameOsx} {config.IP} {config.IP} up" });

            var ip = config.IP.GetAddressBytes();
            ip[^1] = 0;
            Command.Osx(string.Empty, new string[] { $"route add -net {new IPAddress(ip)}/24 {config.IP}" });

            AddRoute();
        }
        private string GetOsxInterfaceNum()
        {
            string output = Command.Osx(string.Empty, new string[] { "ifconfig" });
            var arr = output.Split(Environment.NewLine);
            for (int i = 0; i < arr.Length; i++)
            {
                var item = arr[i];
                if (item.Contains("inet "))
                {
                    for (int k = i; k >= 0; k--)
                    {
                        var itemk = arr[k];
                        if (itemk.Contains("flags=") && itemk.StartsWith("en"))
                        {
                            return itemk.Split(": ")[0];
                        }
                    }
                }

            }
            return string.Empty;
        }

        private void AddRoute()
        {
            foreach (var item in ips)
            {
                VeaLanIPAddress[] _lanips = ExcludeLanIP(item.Value.LanIPs, item.Value.Client);
                AddRoute(_lanips);
            }
        }
        private void AddRoute(VeaLanIPAddress[] ip)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                AddRouteWindows(ip);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                AddRouteLinux(ip);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                AddRouteOsx(ip);
            }
        }
        private void AddRouteWindows(VeaLanIPAddress[] ip)
        {
            if (interfaceNumber > 0)
            {
                List<string> commands = new List<string>(ip.Length);
                foreach (var item in ip)
                {
                    GetMinNetWork(item, out byte mask);
                    byte[] maskArr = BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(0xffffffff << (32 - mask)));
                    commands.Add($"route add {string.Join(".", BinaryPrimitives.ReverseEndianness(item.IPAddress).ToBytes())} mask {string.Join(".", maskArr)} {config.IP} metric 5 if {interfaceNumber}");
                }
                Command.Windows(string.Empty, commands.ToArray());
            }
        }
        private void AddRouteLinux(VeaLanIPAddress[] ip)
        {
            string[] commands = ip.Select(item =>
            {
                GetMinNetWork(item, out byte mask);
                return $"ip route add {string.Join(".", BinaryPrimitives.ReverseEndianness(item.IPAddress).ToBytes())}/{mask} via {config.IP} dev {veaName} metric 1 ";
            }).ToArray();
            Command.Linux(string.Empty, commands);
        }
        private void AddRouteOsx(VeaLanIPAddress[] ip)
        {
            string[] commands = ip.Select(item =>
            {
                GetMinNetWork(item, out byte mask);
                return $"route add -net {string.Join(".", BinaryPrimitives.ReverseEndianness(item.IPAddress).ToBytes())}/{mask} {config.IP}";
            }).ToArray();
            if (commands.Length > 0)
            {
                Command.Osx(string.Empty, commands.ToArray());
            }
        }

        private void DelRoute(VeaLanIPAddress[] ip)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                DelRouteWindows(ip);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                DelRouteLinux(ip);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                DelRouteOsx(ip);
            }
        }
        private void DelRouteWindows(VeaLanIPAddress[] ip)
        {
            if (interfaceNumber > 0)
            {
                string[] commands = ip.Select(item => $"route delete {string.Join(".", BinaryPrimitives.ReverseEndianness(item.IPAddress).ToBytes())}").ToArray();
                Command.Windows(string.Empty, commands.ToArray());
            }
        }
        private void DelRouteLinux(VeaLanIPAddress[] ip)
        {
            string[] commands = ip.Select(item =>
            {
                GetMinNetWork(item, out byte mask);
                return $"ip route del {string.Join(".", BinaryPrimitives.ReverseEndianness(item.IPAddress).ToBytes())}/{mask}";
            }).ToArray();
            Command.Linux(string.Empty, commands);
        }
        private void DelRouteOsx(VeaLanIPAddress[] ip)
        {
            string[] commands = ip.Select(item =>
            {
                GetMinNetWork(item, out byte mask);
                return $"route delete -net {string.Join(".", BinaryPrimitives.ReverseEndianness(item.IPAddress).ToBytes())}/{mask}";
            }).ToArray();
            if (commands.Length > 0)
            {
                Command.Osx(string.Empty, commands.ToArray());
            }
        }

        private void RemoveLanMasks(VeaLanIPAddress[] _lanips)
        {
            foreach (var item in _lanips)
            {
                lanips.TryRemove(GetMinNetWork(item, out _), out _);
            }
            DelRoute(_lanips);
        }

        private void AddLanMasks(IPAddressInfo ips, ClientInfo client)
        {
            var _lanips = ExcludeLanIP(ips.LanIPs, client);
            foreach (var item in _lanips)
            {
                IPAddressCacheInfo cache = new IPAddressCacheInfo
                {
                    Client = client,
                    IP = item.IPAddress,
                    LanIPs = ips.LanIPs,
                    NetWork = GetNetWork(item.IPAddress, item.Mask),
                    Mask = item.Mask
                };
                lanips.AddOrUpdate(GetMinNetWork(item, out _), cache, (a, b) => cache);
            }
        }

        /// <summary>
        /// 排除与目标客户端连接的ip
        /// </summary>
        /// <param name="lanips"></param>
        /// <param name="client"></param>
        /// <returns></returns>
        private VeaLanIPAddress[] ExcludeLanIP(VeaLanIPAddress[] lanips, ClientInfo client)
        {
            IPAddress ip = client.IPAddress.Address;

            //跟目标客户端是局域网连接，则排除连接的ip网段, 连接的ip，与目标传来的局域网ip，进行ip匹配，是否是同网段
            if (client.ConnectType == ClientConnectTypes.P2P && ip.IsLan())
            {
                lanips = lanips.Where(c =>
                {
                    //取网络号不一样的
                    return GetNetWork(BinaryPrimitives.ReadUInt32BigEndian(ip.GetAddressBytes()), c.Mask) != GetNetWork(c.IPAddress, c.Mask);
                }).ToArray();
            }
            return lanips;
        }

        /// <summary>
        /// 重新计算其掩码，不显式定义掩码时，给它计算出一个来
        /// </summary>
        /// <param name="_ips"></param>
        private void ResetMask(IPAddressInfo _ips)
        {
            foreach (var item in _ips.LanIPs)
            {
                if (item.Mask == 0)
                {
                    item.Mask = 32;
                    for (int i = 0; i < sizeof(uint); i++)
                    {
                        if (((item.IPAddress >> (i * 8)) & 0x000000ff) != 0)
                        {
                            break;
                        }
                        item.Mask -= 8;
                    }
                }
            }
        }

        /// <summary>
        /// 获取网络号
        /// </summary>
        /// <param name="ip">小端</param>
        /// <returns>网络号 小端</returns>
        public uint GetNetWork(uint ip, byte maskBitLength)
        {
            if (maskBitLength >= 32 || maskBitLength == 0) return ip;

            return ip & (uint)(1 << (32 - maskBitLength));
        }

        /// <summary>
        /// 最小网络号，整段的 32 24 16 8，用作一段判断，二段判断其具体掩码
        /// </summary>
        /// <param name="ip"></param>
        /// <returns>网络号 小端</returns>
        public uint GetMinNetWork(VeaLanIPAddress ip, out byte maskBitLength)
        {
            uint mask = 0xffffff00;
            maskBitLength = 24;
            for (int i = 1; i < sizeof(uint); i++)
            {
                if (((ip.IPAddress >> (i * 8)) & 0x000000ff) == 0 || maskBitLength > ip.Mask)
                {
                    mask <<= 8;
                    maskBitLength -= 8;
                }
                else break;
            }
            return ip.IPAddress & mask;
        }
    }

    /// <summary>
    /// ip缓存
    /// </summary>
    public sealed class IPAddressCacheInfo
    {
        /// <summary>
        /// ip 小端
        /// </summary>
        public uint IP { get; set; }
        /// <summary>
        /// 局域网网段
        /// </summary>
        public VeaLanIPAddress[] LanIPs { get; set; }
        /// <summary>
        /// 客户端
        /// </summary>

        [System.Text.Json.Serialization.JsonIgnore]
        public ClientInfo Client { get; set; }

        /// <summary>
        /// 网络号，小端
        /// </summary>
        public uint NetWork { get; set; }
        public byte Mask { get; set; }
    }

    /// <summary>
    /// ip更新消息模型
    /// </summary>
    public sealed class IPAddressInfo
    {
        /// <summary>
        /// ip 小端
        /// </summary>
        public uint IP { get; set; }
        /// <summary>
        /// 局域网网段
        /// </summary>
        public VeaLanIPAddress[] LanIPs { get; set; }

        /// <summary>
        /// 序列化
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            var bytes = new byte[1 + 4 + 1 + LanIPs.Length * 5];
            var span = bytes.AsSpan();

            int index = 0;
            bytes[index] = 4;
            index += 1;
            IP.ToBytes(bytes.AsMemory(index));
            index += 4;

            bytes[index] = (byte)LanIPs.Length;
            index += 1;
            for (int i = 0; i < LanIPs.Length; i++)
            {
                LanIPs[i].IPAddress.ToBytes(bytes.AsMemory(index));
                index += 4;
                bytes[index] = LanIPs[i].Mask;
                index += 1;
            }

            return bytes;
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="memory"></param>
        public void DeBytes(ReadOnlyMemory<byte> memory)
        {
            var span = memory.Span;

            int index = 0;

            byte ipLength = span[index];
            index += 1;

            IP = span.Slice(index, ipLength).ToUInt32();
            index += ipLength;

            byte lanipLength = span[index];
            index += 1;

            LanIPs = new VeaLanIPAddress[lanipLength];
            for (int i = 0; i < lanipLength; i++)
            {
                ReadOnlyMemory<byte> ip = memory.Slice(index, 4);
                index += 4;
                byte mask = span[index];
                index += 1;

                LanIPs[i] = new VeaLanIPAddress
                {
                    IPAddress = ip.ToUInt32(),
                    Mask = mask
                };
            }
        }
    }
}
