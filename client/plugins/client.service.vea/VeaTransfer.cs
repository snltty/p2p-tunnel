using client.messengers.clients;
using client.messengers.singnin;
using client.service.vea.socks5;
using common.libs;
using common.libs.extends;
using common.proxy;
using common.server;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading.Tasks;

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

        private readonly ConcurrentDictionary<IPAddress, IPAddressCacheInfo> ips = new ConcurrentDictionary<IPAddress, IPAddressCacheInfo>();
        private readonly ConcurrentDictionary<int, IPAddressCacheInfo> lanips = new ConcurrentDictionary<int, IPAddressCacheInfo>();
        public ConcurrentDictionary<IPAddress, IPAddressCacheInfo> IPList => ips;
        public ConcurrentDictionary<int, IPAddressCacheInfo> LanIPList => lanips;

        private readonly Config config;
        private readonly IClientInfoCaching clientInfoCaching;
        private readonly VeaMessengerSender veaMessengerSender;
        private readonly SignInStateInfo signInStateInfo;
        private readonly IProxyServer proxyServer;

        public VeaTransfer(Config config, IClientInfoCaching clientInfoCaching, VeaMessengerSender veaMessengerSender,  SignInStateInfo signInStateInfo, IProxyServer proxyServer)
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
                if (value.Key != null)
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
                var cache = ips.Values.FirstOrDefault(c => c.Client.Id == client.Id);
                if (cache != null)
                {
                    ips.TryRemove(cache.IP, out _);
                    RemoveLanMasks(cache.LanIPs);
                }

                cache = new IPAddressCacheInfo { Client = client, IP = _ips.IP, LanIPs = _ips.LanIPs };
                ips.AddOrUpdate(_ips.IP, cache, (a, b) => cache);
                AddLanMasks(_ips.LanIPs, cache, client);

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
            foreach (var item in lanips)
            {
                IPAddress[] _lanips = ExcludeLanIP(item.Value.LanIPs, item.Value.Client);
                AddRoute(_lanips);
            }
        }
        private void AddRoute(IPAddress[] ip)
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
        private void AddRouteWindows(IPAddress[] ip)
        {
            if (interfaceNumber > 0)
            {
                List<string> commands = new List<string>(ip.Length);
                foreach (var item in ip)
                {
                    if (item.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        byte[] mask = GetIPMask(item);
                        commands.Add($"route add {item} mask {string.Join(".", mask)} {config.IP} metric 5 if {interfaceNumber}");
                    }
                }
                Command.Windows(string.Empty, commands.ToArray());
            }
        }
        private void AddRouteLinux(IPAddress[] ip)
        {
            string[] commands = ip.Select(c =>
            {
                int mask = c.GetAddressBytes().Count(c => c > 0) * 8;
                return $"ip route add {c}/{mask} via {config.IP} dev {veaName} metric 1 ";
            }).ToArray();
            Command.Linux(string.Empty, commands);
        }
        private void AddRouteOsx(IPAddress[] ip)
        {
            string[] commands = ip.Select(c =>
            {
                int mask = c.GetAddressBytes().Count(c => c > 0) * 8;
                return $"route add -net {c}/{mask} {config.IP}";
            }).ToArray();
            if (commands.Length > 0)
            {
                Command.Osx(string.Empty, commands.ToArray());
            }
        }

        private void DelRoute(IPAddress[] ip)
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
        private void DelRouteWindows(IPAddress[] ip)
        {
            if (interfaceNumber > 0)
            {
                string[] commands = ip.Select(c => $"route delete {c}").ToArray();
                Command.Windows(string.Empty, commands.ToArray());
            }
        }
        private void DelRouteLinux(IPAddress[] ip)
        {
            string[] commands = ip.Select(c =>
            {
                int mask = c.GetAddressBytes().Count(c => c > 0) * 8;
                return $"ip route del {c}/{mask}";
            }).ToArray();
            Command.Linux(string.Empty, commands);
        }
        private void DelRouteOsx(IPAddress[] ip)
        {
            string[] commands = ip.Select(c =>
            {
                int mask = c.GetAddressBytes().Count(c => c > 0) * 8;
                return $"route delete -net {c}/{mask}";
            }).ToArray();
            if (commands.Length > 0)
            {
                Command.Osx(string.Empty, commands.ToArray());
            }
        }

        private void RemoveLanMasks(IPAddress[] _lanips)
        {
            foreach (var item in _lanips)
            {
                lanips.TryRemove(item.GetAddressBytes().ToInt32(), out _);
            }
            DelRoute(_lanips);
        }

        private void AddLanMasks(IPAddress[] _lanips, IPAddressCacheInfo cache, ClientInfo client)
        {
            _lanips = ExcludeLanIP(_lanips, client);
            foreach (var item in _lanips)
            {
                lanips.AddOrUpdate(item.GetAddressBytes().ToInt32(), cache, (a, b) => cache);
            }
        }

        private IPAddress[] ExcludeLanIP(IPAddress[] _lanips, ClientInfo client)
        {
            IPAddress ip = client.IPAddress.Address;

            //跟目标客户端是局域网连接，则排除连接的ip网段
            if (client.ConnectType == ClientConnectTypes.P2P && ip.IsLan())
            {
                _lanips = _lanips.Where(c =>
                {
                    int mask = GetIPMask(c).ToInt32();
                    int _lanip = c.GetAddressBytes().ToInt32() & mask;
                    int _ip = ip.GetAddressBytes().ToInt32() & mask;
                    return _lanip != _ip;
                }).ToArray();
            }
            return _lanips;
        }

        private byte[] GetIPMask(IPAddress ip)
        {
            byte[] mask = new byte[] { 255, 255, 255, 255 };
            byte[] ipBytes = ip.GetAddressBytes();
            for (int i = 0; i < ipBytes.Length; i++)
            {
                if (ipBytes[i] == 0) mask[i] = 0;
            }
            return mask;
        }

    }

    /// <summary>
    /// ip缓存
    /// </summary>
    public sealed class IPAddressCacheInfo
    {
        /// <summary>
        /// ip
        /// </summary>
        public IPAddress IP { get; set; }
        /// <summary>
        /// 局域网网段
        /// </summary>
        public IPAddress[] LanIPs { get; set; }
        /// <summary>
        /// 客户端
        /// </summary>

        [System.Text.Json.Serialization.JsonIgnore]
        public ClientInfo Client { get; set; }
    }

    /// <summary>
    /// ip更新消息模型
    /// </summary>
    public sealed class IPAddressInfo
    {
        /// <summary>
        /// ip
        /// </summary>
        public IPAddress IP { get; set; }
        /// <summary>
        /// 局域网网段
        /// </summary>
        public IPAddress[] LanIPs { get; set; }

        /// <summary>
        /// 序列化
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            var bytes = new byte[1 + 4 + 1 + LanIPs.Length * 4];
            var span = bytes.AsSpan();

            int index = 0;
            bytes[index] = 4;
            index += 1;
            IP.TryWriteBytes(span.Slice(index), out _);
            index += 4;

            bytes[index] = (byte)LanIPs.Length;
            index += 1;
            for (int i = 0; i < LanIPs.Length; i++)
            {
                LanIPs[i].TryWriteBytes(span.Slice(index), out _);
                index += 4;
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

            IP = new IPAddress(span.Slice(index, ipLength));
            index += ipLength;

            byte lanipLength = span[index];
            index += 1;

            LanIPs = new IPAddress[lanipLength];
            for (int i = 0; i < lanipLength; i++)
            {
                LanIPs[i] = new IPAddress(span.Slice(index, 4));
                index += 4;
            }
        }
    }
}
