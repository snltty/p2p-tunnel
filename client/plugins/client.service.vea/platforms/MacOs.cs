using common.libs;
using common.libs.extends;
using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Linq;
using System.Net;

namespace client.service.vea.platforms
{
    public sealed class MacOs : IVeaPlatform
    {
        string interfaceOsx = string.Empty;
        Process Tun2SocksProcess;
        const string veaNameOsx = "utun12138";

        private readonly Config config;
        public MacOs(Config config)
        {
            this.config = config;
        }

        public bool Run()
        {
            interfaceOsx = GetOsxInterfaceNum();
            try
            {
                Tun2SocksProcess = Command.Execute("./tun2socks-osx", $" -device {veaNameOsx} -proxy socks5://127.0.0.1:{config.ListenPort} -interface {interfaceOsx} -loglevel silent");
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex.Message);
                return false;
            }

            for (int i = 0; i < 5; i++)
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

            return string.IsNullOrWhiteSpace(interfaceOsx) == false;
        }
        public void Kill()
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
        public void AddRoute(VeaLanIPAddress[] ip)
        {
            string[] commands = ip.Where(c => c.IPAddress > 0).Select(item =>
            {
                return $"route add -net {string.Join(".", BinaryPrimitives.ReverseEndianness(item.IPAddress).ToBytes())}/{item.MaskLength} {config.IP}";
            }).ToArray();
            if (commands.Length > 0)
            {
                Command.Osx(string.Empty, commands.ToArray());
            }
        }
        public void DelRoute(VeaLanIPAddress[] ip)
        {
            string[] commands = ip.Select(item =>
            {
                return $"route delete -net {string.Join(".", BinaryPrimitives.ReverseEndianness(item.IPAddress).ToBytes())}/{item.MaskLength}";
            }).ToArray();
            if (commands.Length > 0)
            {
                Command.Osx(string.Empty, commands.ToArray());
            }
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
    }
}
