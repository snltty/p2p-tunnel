using common.libs;
using common.libs.extends;
using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Linq;

namespace client.service.vea.platforms
{
    public sealed class Linux : IVeaPlatform
    {
        string interfaceLinux = string.Empty;
        Process Tun2SocksProcess;
        const string veaName = "p2p-tunnel";

        private readonly Config config;
        public Linux(Config config)
        {
            this.config = config;
        }

        public bool Run()
        {
            Command.Linux(string.Empty, new string[] {
                $"ip tuntap add mode tun dev {veaName}",
                $"ip addr add {config.IP}/24 dev {veaName}",
                $"ip link set dev {veaName} up"
            });

            string str = Command.Linux(string.Empty, new string[] { $"ifconfig" });
            if (str.Contains(veaName) == false)
            {
                string msg = Command.Linux(string.Empty, new string[] { $"ip tuntap add mode tun dev {veaName}" });
                Logger.Instance.Error(msg);
                return false;
            }

            interfaceLinux = GetLinuxInterfaceNum();
            try
            {
                string command = $" -device {veaName} -proxy socks5://127.0.0.1:{config.ListenPort} -interface {interfaceLinux} -loglevel silent";
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    Logger.Instance.Warning($"vea linux ->exec:{command}");
                }
                Tun2SocksProcess = Command.Execute("./tun2socks-linux", command);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex.Message);
                return false;
            }

            return string.IsNullOrWhiteSpace(interfaceLinux) == false;
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

            Command.Linux(string.Empty, new string[] { $"ip tuntap del mode tun dev {veaName}" });
        }
        public void AddRoute(VeaLanIPAddress[] ip)
        {
            string[] commands = ip.Where(c => c.IPAddress > 0).Select(item =>
            {
                return $"ip route add {string.Join(".", BinaryPrimitives.ReverseEndianness(item.IPAddress).ToBytes())}/{item.MaskLength} via {config.IP} dev {veaName} metric 1 ";
            }).ToArray();
            if (commands.Length > 0)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    Logger.Instance.Warning($"vea linux ->add route:{string.Join(Environment.NewLine, commands)}");
                }
                Command.Linux(string.Empty, commands);
            }
        }
        public void DelRoute(VeaLanIPAddress[] ip)
        {
            string[] commands = ip.Select(item =>
            {
                return $"ip route del {string.Join(".", BinaryPrimitives.ReverseEndianness(item.IPAddress).ToBytes())}/{item.MaskLength}";
            }).ToArray();

            if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
            {
                Logger.Instance.Warning($"vea linux ->del route:{string.Join(Environment.NewLine, commands)}");
            }
            Command.Linux(string.Empty, commands);
        }

        private string GetLinuxInterfaceNum()
        {
            string output = Command.Linux(string.Empty, new string[] { "ip route" });
            foreach (var item in output.Split(Environment.NewLine))
            {
                if (item.StartsWith("default via"))
                {
                    var strs = item.Split(Helper.SeparatorCharSpace);
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
    }
}
