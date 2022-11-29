using client.service.ui.api.clientServer;
using System.Threading.Tasks;

namespace client.service.logger
{
    public class LoggerClientConfigure : IClientConfigure
    {
        private readonly Config config;
        public LoggerClientConfigure(Config config)
        {
            this.config = config;
        }

        public string Name => "日志信息";

        public string Author => "snltty";

        public string Desc => "收集日志输出到前端";

        public bool Enable => config.Enable;

        public async Task<string> Load()
        {
            return await config.ReadString();
        }

        public async Task<string> Save(string jsonStr)
        {
           
            await config.SaveConfig(jsonStr).ConfigureAwait(false);

            return string.Empty;
        }
    }

}
