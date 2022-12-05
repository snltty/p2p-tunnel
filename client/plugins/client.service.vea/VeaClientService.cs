using client.messengers.clients;
using client.service.ui.api.clientServer;
using common.libs.extends;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace client.service.vea
{
    /// <summary>
    /// 组网前端接口
    /// </summary>
    public sealed class VeaClientService : IClientService
    {
        private readonly Config config;
        private readonly VeaTransfer VeaTransfer;
        private readonly IVeaSocks5ServerHandler veaSocks5ServerHandler;
        private readonly VeaMessengerSender veaMessengerSender;
        private readonly IClientInfoCaching clientInfoCaching;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        /// <param name="VeaTransfer"></param>
        /// <param name="veaSocks5ServerHandler"></param>
        /// <param name="veaMessengerSender"></param>
        /// <param name="clientInfoCaching"></param>
        public VeaClientService(Config config, VeaTransfer VeaTransfer, IVeaSocks5ServerHandler veaSocks5ServerHandler, VeaMessengerSender veaMessengerSender, IClientInfoCaching clientInfoCaching)
        {
            this.config = config;
            this.VeaTransfer = VeaTransfer;
            this.veaSocks5ServerHandler = veaSocks5ServerHandler;
            this.veaMessengerSender = veaMessengerSender;
            this.clientInfoCaching = clientInfoCaching;
        }

        /// <summary>
        /// 获取配置
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public Config Get(ClientServiceParamsInfo arg)
        {
            return config;
        }

        /// <summary>
        /// 保存配置
        /// </summary>
        /// <param name="arg"></param>
        public void Set(ClientServiceParamsInfo arg)
        {
            config.SaveConfig(arg.Content).Wait();

            veaSocks5ServerHandler.UpdateConfig();

            try
            {
                VeaTransfer.Run();
            }
            catch (Exception ex)
            {
                arg.SetCode(ClientServiceResponseCodes.Error, ex.Message);
            }
        }
        /// <summary>
        /// 各个客户端
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public Dictionary<ulong, IPAddressCacheInfo> List(ClientServiceParamsInfo arg)
        {
            return VeaTransfer.IPList.ToDictionary(c => c.Value.Client.Id, d => d.Value);
        }

        /// <summary>
        /// 重装其网卡
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public async Task<bool> Reset(ClientServiceParamsInfo arg)
        {
            ulong id = ulong.Parse(arg.Content);
            if (clientInfoCaching.Get(id, out ClientInfo client))
            {
                await veaMessengerSender.Reset(client.Connection, id);
            }
            return true;
        }

        /// <summary>
        /// 刷新ip列表
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public bool Update(ClientServiceParamsInfo arg)
        {
            VeaTransfer.UpdateIp();
            return true;
        }
    }

}
