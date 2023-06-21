using client.service.ui.api.clientServer;
using common.proxy;
using System;
using System.Threading.Tasks;

namespace client.service.socks5
{
    /// <summary>
    /// socks5
    /// </summary>
    public sealed class Socks5ClientService : IClientService
    {
        private readonly common.socks5.Config config;
        private readonly IProxyServer proxyServer;
        private readonly Socks5Transfer socks5Transfer;

        public Socks5ClientService(common.socks5.Config config, IProxyServer proxyServer, Socks5Transfer socks5Transfer)
        {
            this.config = config;
            this.proxyServer = proxyServer;
            this.socks5Transfer = socks5Transfer;
        }

        /// <summary>
        /// 获取配置
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public common.socks5.Config Get(ClientServiceParamsInfo arg)
        {
            return config;
        }
        /// <summary>
        /// 获取pac
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public string GetPac(ClientServiceParamsInfo arg)
        {
            return socks5Transfer.GetPac();
        }

        /// <summary>
        /// 设置配置
        /// </summary>
        /// <param name="arg"></param>
        public async Task Set(ClientServiceParamsInfo arg)
        {
            await config.SaveConfig(arg.Content);
        }
        public bool Run(ClientServiceParamsInfo arg)
        {
            proxyServer.Stop(config.Plugin);
            if (config.ListenEnable)
            {
                try
                {
                    return proxyServer.Start((ushort)config.ListenPort, config.Plugin);
                }
                catch (Exception ex)
                {
                    arg.SetCode(ClientServiceResponseCodes.Error, ex.Message);
                }
            }
            socks5Transfer.UpdatePac();
            return true;
        }

        /// <summary>
        /// 更新pac内容
        /// </summary>
        /// <param name="arg"></param>
        public void UpdatePac(ClientServiceParamsInfo arg)
        {
            socks5Transfer.UpdatePac(arg.Content);
        }

        public async Task<EnumProxyCommandStatusMsg> Test(ClientServiceParamsInfo arg)
        {
            return await socks5Transfer.Test();
        }
    }
}
