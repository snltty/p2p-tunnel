using common.libs;
using common.libs.extends;
using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Linq;
using System.Net;

namespace client.service.vea.platforms
{
    public sealed class Windows : IVeaPlatform
    {
        int interfaceNumber = 0;
        Process Tun2SocksProcess;
        const string veaName = "p2p-tunnel";

        private readonly Config config;
        public Windows(Config config)
        {
            this.config = config;
        }

        public bool Run()
        {
            for (int i = 0; i < 2; i++)
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
                                if (config.ProxyAll) //代理所有
                                {
                                    //AddRoute(IPAddress.Any);
                                }
                                return true;
                            }
                        }
                    }
                }
                Kill();
            }

            if (interfaceNumber <= 0)
            {
                string msg = Command.Execute("tun2socks-windows.exe", $" -device {veaName} -proxy socks5://127.0.0.1:{config.ListenPort} -loglevel silent", Array.Empty<string>());
                Logger.Instance.Error(msg);
            }

            return interfaceNumber > 0;
        }
        public void Kill()
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
        public void AddRoute(VeaLanIPAddress[] ip)
        {
            if (interfaceNumber > 0)
            {
                string[] commands = ip.Where(c => c.IPAddress > 0).Select(item =>
                {
                    byte[] maskArr = BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(item.MaskValue));
                    return $"route add {string.Join(".", BinaryPrimitives.ReverseEndianness(item.IPAddress).ToBytes())} mask {string.Join(".", maskArr)} {config.IP} metric 5 if {interfaceNumber}";
                }).ToArray();
                if (commands.Length > 0)
                {
                    Command.Windows(string.Empty, commands);
                }
            }
        }
        public void DelRoute(VeaLanIPAddress[] ip)
        {
            if (interfaceNumber > 0)
            {
                string[] commands = ip.Select(item => $"route delete {string.Join(".", BinaryPrimitives.ReverseEndianness(item.IPAddress).ToBytes())}").ToArray();
                Command.Windows(string.Empty, commands.ToArray());
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
    }
}
