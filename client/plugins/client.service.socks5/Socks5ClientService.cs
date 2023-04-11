using client.service.ui.api.clientServer;
using common.socks5;
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
        private readonly ISocks5ClientListener socks5ClientListener;
        private readonly Socks5Transfer socks5Transfer;

        public Socks5ClientService(common.socks5.Config config, ISocks5ClientListener socks5ClientListener, Socks5Transfer socks5Transfer)
        {
            this.config = config;
            this.socks5ClientListener = socks5ClientListener;
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
        public void Run(ClientServiceParamsInfo arg)
        {
            socks5ClientListener.Stop();
            if (config.ListenEnable)
            {
                try
                {
                    socks5ClientListener.Start(config.ListenPort, config.BufferSize);
                }
                catch (Exception ex)
                {
                    arg.SetCode(ClientServiceResponseCodes.Error, ex.Message);
                }
            }
            socks5Transfer.UpdatePac();
        }



        /// <summary>
        /// 更新pac内容
        /// </summary>
        /// <param name="arg"></param>
        public void UpdatePac(ClientServiceParamsInfo arg)
        {
            socks5Transfer.UpdatePac(arg.Content);
        }

    }
}
