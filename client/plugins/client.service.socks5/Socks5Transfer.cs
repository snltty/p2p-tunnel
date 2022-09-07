using common.libs;
using System;
using System.IO;

namespace client.service.socks5
{
    public class Socks5Transfer
    {
        private readonly common.socks5.Config config;
        private readonly ui.api.Config uiconfig;
        public Socks5Transfer(common.socks5.Config config, ui.api.Config uiconfig)
        {
            this.config = config;
            this.uiconfig = uiconfig;

            AppDomain.CurrentDomain.ProcessExit += (s, e) => ClearPac();
            //安卓注释
            //Console.CancelKeyPress += (s, e) => ClearPac();
        }

        public string GetPac()
        {
            try
            {
                return File.ReadAllText("./socks-custom.pac");
            }
            catch (Exception)
            {
            }
            return String.Empty;
        }

        public string UpdatePac(PacSetParamsInfo param)
        {
            try
            {
                string pacContent = param.Pac;
                string file = Path.Join(uiconfig.Web.Root, "socks-custom.pac");
                if (param.IsCustom)
                {
                    if (string.IsNullOrEmpty(param.Pac))
                    {
                        pacContent = File.ReadAllText("./socks-custom.pac");
                    }
                    else
                    {
                        File.WriteAllText("./socks-custom.pac", param.Pac);
                    }
                }
                else
                {
                    file = Path.Join(uiconfig.Web.Root, "socks.pac");
                    pacContent = File.ReadAllText("./socks.pac");
                }

                pacContent = pacContent.Replace("{socks5-address}", $"127.0.0.1:{config.ListenPort}");
                File.WriteAllText(file, pacContent);

                if (config.ListenEnable)
                {
                    SetPac($"http://{uiconfig.Web.BindIp}:{uiconfig.Web.Port}/{Path.GetFileName(file)}");
                }
                else
                {
                    ClearPac();
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public void SetPac(string url)
        {
            ProxySystemSetting.Set(url);
        }

        public void ClearPac()
        {
            ProxySystemSetting.Clear();
        }
    }

    public class PacSetParamsInfo
    {
        public bool IsCustom { get; set; } = false;
        public string Pac { get; set; } = string.Empty;
    }
}
