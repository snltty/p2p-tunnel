using common.libs;
using System;
using System.IO;

namespace client.service.socks5
{
    /// <summary>
    /// socks5
    /// </summary>
    public sealed class Socks5Transfer
    {
        private readonly common.socks5.Config config;
        private readonly ui.api.Config uiconfig;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        /// <param name="uiconfig"></param>
        public Socks5Transfer(common.socks5.Config config, ui.api.Config uiconfig)
        {
            this.config = config;
            this.uiconfig = uiconfig;

            AppDomain.CurrentDomain.ProcessExit += (s, e) => ClearPac();
            //安卓注释
            //Console.CancelKeyPress += (s, e) => ClearPac();
        }

        /// <summary>
        /// 获取pac
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// 更新pac
        /// </summary>
        /// <returns></returns>
        public string UpdatePac()
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

                pacContent = pacContent.Replace("{socks5-address}", $"127.0.0.1:{config.ListenPort}");
                File.WriteAllText(file, pacContent);

                if (config.ListenEnable && config.IsPac)
                {
                    SetPac($"http://{(uiconfig.Web.BindIp == "+" ? "127.0.0.1" : uiconfig.Web.BindIp)}:{uiconfig.Web.Port}/{Path.GetFileName(file)}");
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

        public void UpdatePac(string content)
        {
            File.WriteAllText("./socks-custom.pac", content);
        }

        /// <summary>
        /// 更新pac
        /// </summary>
        /// <param name="url"></param>
        public void SetPac(string url)
        {
            ProxySystemSetting.Set(url);
        }

        /// <summary>
        /// 清除pac
        /// </summary>
        public void ClearPac()
        {
            ProxySystemSetting.Clear();
        }
    }

    /// <summary>
    /// 设置pac
    /// </summary>
    public class PacSetParamsInfo
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
