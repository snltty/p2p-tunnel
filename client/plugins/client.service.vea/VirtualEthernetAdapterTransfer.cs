using client.messengers.clients;
using common.libs;
using common.socks5;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading.Tasks;

namespace client.service.vea
{
    public class VirtualEthernetAdapterTransfer
    {
        Process Tun2SocksProcess;
        const string veaName = "p2p-tunnel";
        private readonly ConcurrentDictionary<ulong, IPAddress> ips = new ConcurrentDictionary<ulong, IPAddress>();
        private readonly ConcurrentDictionary<IPAddress, ClientInfo> ips2 = new ConcurrentDictionary<IPAddress, ClientInfo>();
        public ConcurrentDictionary<ulong, IPAddress> IPList => ips;
        public ConcurrentDictionary<IPAddress, ClientInfo> IPList2 => ips2;

        private readonly Config config;
        private readonly IVeaSocks5ClientListener socks5ClientListener;

        public VirtualEthernetAdapterTransfer(Config config, IClientInfoCaching clientInfoCaching, VeaMessengerSender veaMessengerSender, IVeaSocks5ClientListener socks5ClientListener)
        {
            this.config = config;
            this.socks5ClientListener = socks5ClientListener;

            clientInfoCaching.OnOnline.Sub((client) =>
            {
                Task.Run(async () =>
                {
                    IPAddress ip = await veaMessengerSender.IP(client.OnlineConnection);
                    ips.AddOrUpdate(client.Id, ip, (a, b) => ip);
                    ips2.AddOrUpdate(ip, client, (a, b) => client);
                });
            });
            clientInfoCaching.OnOffline.Sub((client) =>
            {
                if (ips.TryRemove(client.Id, out IPAddress ip))
                {
                    ips2.TryRemove(ip, out _);
                }
            });

            AppDomain.CurrentDomain.ProcessExit += (object sender, EventArgs e) => KillWindows();
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
        }

        private void KillWindows()
        {
            if (Tun2SocksProcess != null)
            {
                Tun2SocksProcess.Kill();
                Tun2SocksProcess.Close();
                Tun2SocksProcess.Dispose();
                Tun2SocksProcess = null;
            }
        }
        private void RunWindows()
        {
            Tun2SocksProcess = Command.Execute("tun2socks.exe", $" -device {veaName} -proxy socks5://127.0.0.1:{config.SocksPort} -loglevel silent");
            for (int i = 0; i < 60; i++)
            {
                //分配ip
                Command.Execute("cmd.exe", string.Empty, new string[] { $"netsh interface ip set address name=\"{veaName}\" source=static addr={config.IP} mask=255.255.255.0 gateway=none" });
                //网卡编号
                int num = GetWindowsInterfaceNum();
                if (num > 0)
                {
                    if (config.ProxyAll) //代理所有
                    {
                        // Command.Execute("cmd.exe", string.Empty, new string[] { $"route add 0.0.0.0 mask 0.0.0.0 {config.IP} metric 5 if {num}" });
                    }
                    break;
                }
                else
                {
                    System.Threading.Thread.Sleep(1000);
                }
            }
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
    }
}
