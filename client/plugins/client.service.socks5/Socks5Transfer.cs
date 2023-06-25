using client.service.ui.api.clientServer;
using common.libs;
using common.libs.extends;
using common.proxy;
using System;
using System.Buffers;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

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
            Console.CancelKeyPress += (s, e) =>
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
                /*
                if (File.Exists(Path.Join(uiconfig.Web.Root, "socks-custom.pac")))
                {
                    return File.ReadAllText((Path.Join(uiconfig.Web.Root, "socks-custom.pac"));
                }
                */
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
                        file = Path.Join(uiconfig.Web.Root, "socks-custom.pac");
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

        public async Task<EnumProxyCommandStatusMsg> Test()
        {
            if (config.ListenEnable == false)
            {
                return EnumProxyCommandStatusMsg.Listen;
            }
            if (ProxyPluginLoader.GetPlugin(config.Plugin, out IProxyPlugin plugin) == false)
            {
                return EnumProxyCommandStatusMsg.Listen;
            }

            IPEndPoint target = new IPEndPoint(config.ProxyIp, config.ListenPort);
            try
            {
                using Socket socket = new Socket(target.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                await socket.ConnectAsync(target);

                byte[] bytes = new byte[ProxyHelper.MagicData.Length];
                //request
                await socket.SendAsync(new byte[] { 0x05, 0x01, 0 }, SocketFlags.None);
                await socket.ReceiveAsync(bytes, SocketFlags.None);
                //command
                byte[] socks5Data = new byte[] { 0x05, 0x01, 0, 0x01, 8, 8, 8, 8, 0,53 };
                byte[] data = new byte[bytes.Length + socks5Data.Length];
                socks5Data.AsSpan().CopyTo(data);
                ProxyHelper.MagicData.AsSpan().CopyTo(data.AsSpan(socks5Data.Length));
                await socket.SendAsync(data, SocketFlags.None);

                int length = await socket.ReceiveAsync(bytes, SocketFlags.None);
                socket.SafeClose();

                EnumProxyCommandStatusMsg statusMsg = EnumProxyCommandStatusMsg.Listen;
                if (length > 0)
                {
                    statusMsg = (EnumProxyCommandStatusMsg)bytes[0];
                }
                return statusMsg;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex + "");
            }
            return EnumProxyCommandStatusMsg.Listen;
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
