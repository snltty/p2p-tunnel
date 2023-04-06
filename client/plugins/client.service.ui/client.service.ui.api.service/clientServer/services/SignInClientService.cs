using client.messengers.singnin;
using client.service.ui.api.clientServer;
using common.libs.extends;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace client.service.ui.api.service.clientServer.services
{
    /// <summary>
    /// 注册
    /// </summary>
    public sealed class SignInClientService : IClientService
    {

        private readonly ISignInTransfer signinTransfer;
        private readonly SignInStateInfo signInStateInfo;
        private readonly client.Config config;
        public SignInClientService(ISignInTransfer signinTransfer, SignInStateInfo signInStateInfo, client.Config config)
        {
            this.signinTransfer = signinTransfer;
            this.signInStateInfo = signInStateInfo;
            this.config = config;
        }

        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public async Task<bool> Start(ClientServiceParamsInfo arg)
        {
            var result = await signinTransfer.SignIn().ConfigureAwait(false);
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
            signinTransfer.Exit();
        }
        /// <summary>
        /// 获取配置文件和信息
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public SignInInfo Info(ClientServiceParamsInfo arg)
        {
            return new SignInInfo
            {
                ClientConfig = config.Client,
                ServerConfig = config.Server,
                LocalInfo = signInStateInfo.LocalInfo,
                RemoteInfo = signInStateInfo.RemoteInfo,
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

        /// <summary>
        /// ping ip地址
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public async Task<long[]> Ping(ClientServiceParamsInfo arg)
        {
            string[] ips = arg.Content.DeJson<string[]>();

            PingModel[] result = ips.Select(c =>
            {
                Ping ping = new Ping();
                return new PingModel
                {
                    Ping = ping,
                    Task = ping.SendPingAsync(c, 1000)
                };
            }).ToArray();
            await Task.WhenAll(result.Select(c => c.Task));
            foreach (var item in result)
            {
                item.Ping.Dispose();
            }

            return result.Select(c =>
            {
                return c.Task.Result.Status == IPStatus.Success ? c.Task.Result.RoundtripTime : -1;
            }).ToArray();
        }

    }
    /// <summary>
    /// 配置信息
    /// </summary>
    public sealed class ConfigureParamsInfo
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
    public sealed class SignInInfo
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


    class PingModel
    {
        public Ping Ping { get; set; }
        public Task<PingReply> Task { get; set; }

    }
}
