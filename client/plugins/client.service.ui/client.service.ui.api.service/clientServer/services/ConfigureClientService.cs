using client.service.ui.api.clientServer;
using common.libs.extends;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace client.service.ui.api.service.clientServer.services
{
    /// <summary>
    /// 前端配置文件修改接口
    /// </summary>
    public sealed class ConfigureClientService : IClientService
    {
        private readonly IClientServer clientServer;
        private readonly client.Config clientConfig;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientServer"></param>
        public ConfigureClientService(IClientServer clientServer, client.Config clientConfig)
        {
            this.clientServer = clientServer;
            this.clientConfig = clientConfig;
        }

        /// <summary>
        /// 获取配置列表
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public IEnumerable<ClientServiceConfigureInfo> Configures(ClientServiceParamsInfo arg)
        {
            return clientServer.GetConfigures();
        }
        /// <summary>
        /// 获取配置
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public async Task<string> Configure(ClientServiceParamsInfo arg)
        {
            var plugin = clientServer.GetConfigure(arg.Content);
            if (plugin != null)
            {
                return await plugin.Load().ConfigureAwait(false);
            }
            return string.Empty;
        }
        /// <summary>
        /// 保存配置
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public async Task Save(ClientServiceParamsInfo arg)
        {
            SaveParamsInfo model = arg.Content.DeJson<SaveParamsInfo>();
            var plugin = clientServer.GetConfigure(model.ClassName);
            if (plugin != null)
            {
                string msg = await plugin.Save(model.Content).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(msg) == false)
                {
                    arg.SetCode(ClientServiceResponseCodes.Error, msg);
                }
            }
        }
        /// <summary>
        /// 获取服务列表
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public IEnumerable<string> Services(ClientServiceParamsInfo arg)
        {
            if (clientConfig.Client.Services.Length > 0)
            {
                return clientServer.GetServices().Intersect(clientConfig.Client.Services).Append(clientConfig.Client.Services[0]).Reverse();
            }
            return clientServer.GetServices();
        }
    }
    /// <summary>
    /// 保存
    /// </summary>
    public class SaveParamsInfo
    {
        /// <summary>
        /// 类名
        /// </summary>
        public string ClassName { get; set; }
        /// <summary>
        /// 配置内容
        /// </summary>
        public string Content { get; set; }
    }
}
