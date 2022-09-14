using client.messengers.clients;
using common.libs;
using common.libs.extends;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace client.service.vea
{
    public class VeaTransfer
    {
        Process Tun2SocksProcess;
        int interfaceNumber = 0;
        const string veaName = "p2p-tunnel";

        private readonly ConcurrentDictionary<IPAddress, IPAddressCacheInfo> ips = new ConcurrentDictionary<IPAddress, IPAddressCacheInfo>();
        private readonly ConcurrentDictionary<int, IPAddressCacheInfo> lanips = new ConcurrentDictionary<int, IPAddressCacheInfo>();
        public ConcurrentDictionary<IPAddress, IPAddressCacheInfo> IPList => ips;
        public ConcurrentDictionary<int, IPAddressCacheInfo> LanIPList => lanips;

        private readonly Config config;
        private readonly IVeaSocks5ClientListener socks5ClientListener;

        public VeaTransfer(Config config, IClientInfoCaching clientInfoCaching, VeaMessengerSender veaMessengerSender, IVeaSocks5ClientListener socks5ClientListener)
        {
            this.config = config;
            this.socks5ClientListener = socks5ClientListener;

            clientInfoCaching.OnOnline.Sub((client) =>
            {
                veaMessengerSender.IP(client.OnlineConnection).ContinueWith((result) =>
                {
                    IPAddress ip = result.Result.Item1;
                    IPAddress lanip = result.Result.Item2;

                    IPAddressCacheInfo cache = new IPAddressCacheInfo { Client = client, IP = ip, LanIP = lanip, Mask = 0 };

                    ips.AddOrUpdate(ip, cache, (a, b) => cache);
                    if (lanip.Equals(IPAddress.Any) == false)
                    {
                        int mask = GetIpMask(lanip);
                        if (lanips.ContainsKey(mask) == false)
                        {
                            cache.Mask = mask;
                            lanips.TryAdd(mask, cache);
                            AddRoute(lanip);
                        }
                    }
                });
            });
            clientInfoCaching.OnOffline.Sub((client) =>
            {
                var value = ips.FirstOrDefault(c => c.Value.Client.Id == client.Id);
                if (value.Key != null)
                {
                    if (ips.TryRemove(value.Key, out IPAddressCacheInfo cache))
                    {
                        lanips.TryRemove(cache.Mask, out _);
                    }
                }
            });

            AppDomain.CurrentDomain.ProcessExit += (object sender, EventArgs e) => Stop();
        }

        public void Run()
        {
            Stop();
            if (config.Enable)
            {
                RunTun2Socks();
                socks5ClientListener.Start(config.SocksPort, config.BufferSize);
            }
        }
        public void Stop()
        {
            socks5ClientListener.Stop();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                KillWindows();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                KillLinux();
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
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                RunLinux();
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

            Command.Execute("bash", string.Empty, new string[] { $"ip tuntap del mode tun dev {veaName}" });
        }

        private void RunWindows()
        {
            Tun2SocksProcess = Command.Execute("tun2socks-windows.exe", $" -device {veaName} -proxy socks5://127.0.0.1:{config.SocksPort} ");
            for (int i = 0; i < 60; i++)
            {
                //分配ip
                Command.Execute("cmd.exe", string.Empty, new string[] { $"netsh interface ip set address name=\"{veaName}\" source=static addr={config.IP} mask=255.255.255.0 gateway=none" });
                //网卡编号
                interfaceNumber = GetWindowsInterfaceNum();
                if (interfaceNumber > 0)
                {
                    AddRoute();
                    if (config.ProxyAll) //代理所有
                    {
                        //AddRoute(IPAddress.Any);
                    }
                    break;
                }
                else
                {
                    System.Threading.Thread.Sleep(1000);
                }
            }
        }
        private void RunLinux()
        {
            Command.Execute("bash", string.Empty, new string[] {
                $"ip tuntap add mode tun dev {veaName}",
                $"ip addr add {config.IP}/24 dev {veaName}",
                $"ip link set dev {veaName} up"
            });
            Tun2SocksProcess = Command.Execute("bash", $" tun2socks-linux -device {veaName} -proxy socks5://127.0.0.1:{config.SocksPort} -loglevel silent");
        }
        private int GetWindowsInterfaceNum()
        {
            string output = Command.Execute("cmd.exe", string.Empty, new string[] { "route print" });
            foreach (var item in output.Split("\r\n"))
            {
                if (item.Contains("WireGuard Tunnel"))
                {
                    return int.Parse(item.Substring(0, item.IndexOf('.')).Trim());
                }
            }
            return 0;
        }

        private void AddRoute()
        {
            foreach (var item in lanips)
            {
                AddRoute(item.Value.LanIP);
            }
        }
        private void AddRoute(IPAddress ip)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                AddRouteWindows(ip);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                AddRouteLinux(ip);
            }
        }
        private void AddRouteWindows(IPAddress ip)
        {
            if (interfaceNumber > 0)
            {
                Command.Execute("cmd.exe", string.Empty, new string[] { $"route add {ip} mask 255.255.255.0 {config.IP} metric 5 if {interfaceNumber}" });
            }
        }
        private void AddRouteLinux(IPAddress ip)
        {
            Command.Execute("bash", string.Empty, new string[] { $"ip route add {ip} via {config.IP} dev {veaName} metric 1 " });
        }

        public int GetIpMask(IPAddress ip)
        {
            return GetIpMask(ip.GetAddressBytes());
        }
        public int GetIpMask(Span<byte> ip)
        {
            return ip.ToInt32() & 0xffffff;
        }
    }

    public class IPAddressCacheInfo
    {
        public int Mask { get; set; }
        public IPAddress IP { get; set; }
        public IPAddress LanIP { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public ClientInfo Client { get; set; }
    }
}
