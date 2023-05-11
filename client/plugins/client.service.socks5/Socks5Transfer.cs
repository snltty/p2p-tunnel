using common.libs;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace client.service.socks5
{
    /// <summary>
    /// socks5
    /// </summary>
    public sealed class Socks5Transfer
    {
        private readonly common.socks5.Config config;
        private readonly ui.api.Config uiconfig;
        bool set = false;

        public Socks5Transfer(common.socks5.Config config, ui.api.Config uiconfig)
        {
            this.config = config;
            this.uiconfig = uiconfig;

            AppDomain.CurrentDomain.ProcessExit += (s, e) =>
            {
                if (set)
                {
                    ClearPac();
                }
            };
            //安卓注释
            //Console.CancelKeyPress += (s, e) => ClearPac();
        }

        /// <summary>
        /// 获取pac
        /// </summary>
        /// <returns></returns>
        public string GetPac()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return File.ReadAllText("./socks-custom.pac");
            }
            return string.Empty;
        }

        /// <summary>
        /// 更新pac
        /// </summary>
        /// <returns></returns>
        public string UpdatePac()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                try
                {
                    string pacContent = string.Empty;
                    string file = string.Empty;
                    if (config.IsCustomPac)
                    {
                        file = Path.Join(uiconfig.Web.Root, "socks-custom.pac"); ;
                        pacContent = File.ReadAllText("./socks-custom.pac");
                    }
                    else
                    {
                        file = Path.Join(uiconfig.Web.Root, "socks.pac");
                        pacContent = File.ReadAllText("./socks.pac");
                    }

                    pacContent = pacContent.Replace("{socks5-address}", $"{config.ProxyIp}:{config.ListenPort}");
                    File.WriteAllText(file, pacContent);

                    if (config.ListenEnable && config.IsPac)
                    {
                        SetPac($"http://{(uiconfig.Web.BindIp == "+" ? config.ProxyIp.ToString() : uiconfig.Web.BindIp)}:{uiconfig.Web.Port}/{Path.GetFileName(file)}");
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
            return string.Empty;
        }

        public void UpdatePac(string content)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                File.WriteAllText("./socks-custom.pac", content);
            }
        }

        /// <summary>
        /// 更新pac
        /// </summary>
        /// <param name="url"></param>
        public void SetPac(string url)
        {
            set = true;
            ProxySystemSetting.Set(url);
        }

        /// <summary>
        /// 清除pac
        /// </summary>
        public void ClearPac()
        {
            set = false;
            ProxySystemSetting.Clear();
        }
    }

    /// <summary>
    /// 设置pac
    /// </summary>
    public sealed class PacSetParamsInfo
    {
        /// <summary>
        /// 是否自定义
        /// </summary>
        public bool IsCustom { get; set; } = false;
        /// <summary>
        /// pac内容
        /// </summary>
        public string Pac { get; set; } = string.Empty;
    }
}
