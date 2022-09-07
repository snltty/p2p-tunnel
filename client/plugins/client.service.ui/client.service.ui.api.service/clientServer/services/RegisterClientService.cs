using client.messengers.register;
using client.service.ui.api.clientServer;
using common.libs.extends;
using System.Threading.Tasks;

namespace client.service.ui.api.service.clientServer.services
{
    public class RegisterClientService : IClientService
    {
        private readonly IRegisterTransfer registerTransfer;
        private readonly RegisterStateInfo registerState;
        private readonly client.Config config;
        public RegisterClientService(IRegisterTransfer registerHelper, RegisterStateInfo registerState, client.Config config)
        {
            this.registerTransfer = registerHelper;
            this.registerState = registerState;
            this.config = config;
        }

        public async Task<bool> Start(ClientServiceParamsInfo arg)
        {
            var result = await registerTransfer.Register().ConfigureAwait(false);
            if (!result.Data)
            {
                arg.SetCode(ClientServiceResponseCodes.Error, result.ErrorMsg);
            }
            return result.Data;
        }

        public void Stop(ClientServiceParamsInfo arg)
        {
            registerTransfer.Exit();
        }

        public RegisterInfo Info(ClientServiceParamsInfo arg)
        {
            return new RegisterInfo
            {
                ClientConfig = config.Client,
                ServerConfig = config.Server,
                LocalInfo = registerState.LocalInfo,
                RemoteInfo = registerState.RemoteInfo,
            };
        }

        public async Task Config(ClientServiceParamsInfo arg)
        {
            ConfigureParamsInfo model = arg.Content.DeJson<ConfigureParamsInfo>();

            config.Client = model.ClientConfig;
            config.Server = model.ServerConfig;
            await config.SaveConfig().ConfigureAwait(false);
        }
    }

    public class ConfigureParamsInfo
    {
        public ClientConfig ClientConfig { get; set; } = new ClientConfig();
        public ServerConfig ServerConfig { get; set; } = new ServerConfig();
    }

    public class RegisterInfo
    {
        public ClientConfig ClientConfig { get; set; } = new ClientConfig();
        public ServerConfig ServerConfig { get; set; } = new ServerConfig();
        public LocalInfo LocalInfo { get; set; } = new LocalInfo();
        public RemoteInfo RemoteInfo { get; set; } = new RemoteInfo();
    }
}
