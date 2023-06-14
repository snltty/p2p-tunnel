using common.libs;
using common.proxy;
using System;
using System.Buffers;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace client.service.httpProxy
{
    /// <summary>
    /// tcp转发中转器和入口
    /// </summary>
    public sealed class HttpProxyTransfer
    {
        private readonly common.httpProxy.Config config;
        private readonly client.service.ui.api.Config uiconfig;
        private readonly IProxyServer proxyServer;
        public HttpProxyTransfer(common.httpProxy.Config config, ui.api.Config uiconfig, IProxyServer proxyServer)
        {
            this.config = config;
            this.uiconfig = uiconfig;
            this.proxyServer = proxyServer;

            AppDomain.CurrentDomain.ProcessExit += (s, e) =>
            {
                if (set == true)
                {
                    ClearPac();
                }
            };
        }

        bool set = false;

        public bool Update()
        {
            string pacContent = string.Empty;
            string file = Path.Join(uiconfig.Web.Root, "proxy-custom.pac");
            if (config.IsCustomPac)
            {
                pacContent = File.ReadAllText("./proxy-custom.pac");
            }
            else
            {
                file = Path.Join(uiconfig.Web.Root, "proxy.pac");
                pacContent = File.ReadAllText("./proxy.pac");
            }

            pacContent = pacContent.Replace("{proxy-address}", $"{config.ProxyIp}:{config.ListenPort}");
            File.WriteAllText(file, pacContent);

            if (config.ListenEnable && config.IsPac)
            {
                SetPac($"http://{(uiconfig.Web.BindIp == "+" ? config.ProxyIp.ToString() : uiconfig.Web.BindIp)}:{uiconfig.Web.Port}/{Path.GetFileName(file)}");
            }
            else
            {
                ClearPac();
            }

            if (config.ListenEnable)
            {
              return  proxyServer.Start(config.ListenPort, config.Plugin);
            }
            else
            {
                proxyServer.Stop(config.Plugin);
            }
            return true;
        }

        /// <summary>
        /// 获取pac
        /// </summary>
        /// <returns></returns>
        public string GetPac()
        {
            try
            {
                return File.ReadAllText("./proxy-custom.pac");
            }
            catch (Exception)
            {
            }
            return string.Empty;
        }
        /// <summary>
        /// 更新pac
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public string UpdatePac(string pac)
        {
            try
            {
                File.WriteAllText("./proxy-custom.pac", pac);

                return string.Empty;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        /// <summary>
        /// 设置系统pac
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
                await socket.SendAsync(Encoding.UTF8.GetBytes($"CONNECT- / HTTP/1.1\r\nHost: www.baidu.com:443\r\n\r\n"), SocketFlags.None);
                byte[] bytes = ArrayPool<byte>.Shared.Rent(ProxyHelper.MagicData.Length);
                int length = await socket.ReceiveAsync(bytes, SocketFlags.None);

                EnumProxyCommandStatusMsg statusMsg = EnumProxyCommandStatusMsg.Listen;
                if (length > 0)
                {
                    statusMsg = (EnumProxyCommandStatusMsg)bytes[0];
                }
                ArrayPool<byte>.Shared.Return(bytes);
                return statusMsg;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex + "");
            }
            return EnumProxyCommandStatusMsg.Listen;
        }
    }
}