using client.messengers.register;
using client.service.ui.api.clientServer;
using common.libs.extends;
using System.Threading.Tasks;

namespace client.service.ui.api.service.clientServer.services
{
    /// <summary>
    /// 注册
    /// </summary>
    public sealed class RegisterClientService : IClientService
    {

        private readonly IRegisterTransfer registerTransfer;
        private readonly RegisterStateInfo registerStateInfo;
        private readonly client.Config config;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="registerTransfer"></param>
        /// <param name="registerStateInfo"></param>
        /// <param name="config"></param>
        public RegisterClientService(IRegisterTransfer registerTransfer, RegisterStateInfo registerStateInfo, client.Config config)
        {
            this.registerTransfer = registerTransfer;
            this.registerStateInfo = registerStateInfo;
            this.config = config;
        }

        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public async Task<bool> Start(ClientServiceParamsInfo arg)
        {
            var result = await registerTransfer.Register().ConfigureAwait(false);
            if (!result.Data)
            {
                arg.SetCode(ClientServiceResponseCodes.Error, result.ErrorMsg);
            }
            return result.Data;
        }
        /// <summary>
        /// 退出
        /// </summary>
        /// <param name="arg"></param>
        public void Exit(ClientServiceParamsInfo arg)
        {
            registerTransfer.Exit();
        }
        /// <summary>
        /// 获取配置文件和信息
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public RegisterInfo Info(ClientServiceParamsInfo arg)
        {
            return new RegisterInfo
            {
                ClientConfig = config.Client,
                ServerConfig = config.Server,
                LocalInfo = registerStateInfo.LocalInfo,
                RemoteInfo = registerStateInfo.RemoteInfo,
            };
        }
        /// <summary>
        /// 更新配置文件
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public async Task Config(ClientServiceParamsInfo arg)
        {
            ConfigureParamsInfo model = arg.Content.DeJson<ConfigureParamsInfo>();

            config.Client = model.ClientConfig;
            config.Server = model.ServerConfig;

            await config.SaveConfig(config).ConfigureAwait(false);
        }

    }
    /// <summary>
    /// 配置信息
    /// </summary>
    public class ConfigureParamsInfo
    {
        /// <summary>
        /// 客户端配置信息
        /// </summary>
        public ClientConfig ClientConfig { get; set; } = new ClientConfig();
        /// <summary>
        /// 服务端配置信息
        /// </summary>
        public ServerConfig ServerConfig { get; set; } = new ServerConfig();
    }

    /// <summary>
    /// 注册信息
    /// </summary>
    public class RegisterInfo
    {
        /// <summary>
        /// 客户端配置
        /// </summary>
        public ClientConfig ClientConfig { get; set; } = new ClientConfig();
        /// <summary>
        /// 服务端配置
        /// </summary>
        public ServerConfig ServerConfig { get; set; } = new ServerConfig();
        /// <summary>
        /// 本地数据
        /// </summary>
        public LocalInfo LocalInfo { get; set; } = new LocalInfo();
        /// <summary>
        /// 远程数据
        /// </summary>
        public RemoteInfo RemoteInfo { get; set; } = new RemoteInfo();
    }
}
