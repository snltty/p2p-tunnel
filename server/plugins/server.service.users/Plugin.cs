using Microsoft.Extensions.DependencyInjection;
using common.libs;
using common.server;
using System.Reflection;

namespace server.service.users
{
    public sealed class Plugin : IPlugin
    {
        public void LoadAfter(ServiceProvider services, Assembly[] assemblys)
        {
            var config = services.GetService<Config>();
            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));
            Logger.Instance.Info("账号模块已加载");
            if (config.Enable)
            {
                Logger.Instance.Debug($"已启用账号验证");
            }
            else
            {
                Logger.Instance.Info($"未启用账号验证");
            }
            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));
        }

        public void LoadBefore(ServiceCollection services, Assembly[] assemblys)
        {
            services.AddSingleton<Config>();
            services.AddSingleton<IUserStore,UserStore>();
        }
    }
}
