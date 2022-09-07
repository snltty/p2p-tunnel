using client.service.ui.api.clientServer;
using common.libs.extends;
using System.Threading.Tasks;

namespace client.service.ftp.client
{
    public class FtpClientConfigure : IClientConfigure
    {
        private readonly Config config;
        public FtpClientConfigure(Config config)
        {
            this.config = config;
        }

        public string Name => "文件服务";

        public string Author => "snltty";

        public string Desc => "文件上传下载服务";

        public bool Enable => config.Enable;

        public async Task<object> Load()
        {
            return await Task.FromResult(config).ConfigureAwait(false);
        }

        public async Task<string> Save(string jsonStr)
        {
            Config _config = jsonStr.DeJson<Config>();

            config.Password = _config.Password;
            config.ServerRoot = _config.ServerRoot;
            config.ServerCurrentPath = config.ServerRoot;
            config.Enable = _config.Enable;
            config.UploadNum = _config.UploadNum;
            await config.SaveConfig().ConfigureAwait(false);

            return string.Empty;
        }

        public async Task<bool> SwitchEnable(bool enable)
        {
            config.Enable = enable;
            await config.SaveConfig().ConfigureAwait(false);
            return true;
        }
    }


}
