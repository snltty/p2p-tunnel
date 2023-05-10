using client.service.ui.api.clientServer;
using common.libs.extends;
using System.Threading.Tasks;

namespace client.service.users.server
{
    public sealed class UsersConfigure : IClientConfigure
    {
        private readonly common.user.Config config;

        public UsersConfigure(common.user.Config config)
        {
            this.config = config;
        }

        public string Name => "客户端用户管理";
        public string Author => "snltty";
        public string Desc => string.Empty;
        public bool Enable => true;

        /// <summary>
        /// 加载
        /// </summary>
        /// <returns></returns>
        public async Task<string> Load()
        {
            await Task.CompletedTask;
            return config.ToJson();
        }

        /// <summary>
        /// 保存
        /// </summary>
        /// <param name="jsonStr"></param>
        /// <returns></returns>
        public async Task<string> Save(string jsonStr)
        {
            await config.SaveConfig(jsonStr);
            return string.Empty;
        }
    }
}
