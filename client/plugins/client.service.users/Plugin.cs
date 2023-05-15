using Microsoft.Extensions.DependencyInjection;
using common.server;
using System.Reflection;
using common.user;
using common.libs;

namespace client.service.users
{
    public sealed class Plugin : IPlugin
    {
        public void LoadAfter(ServiceProvider services, Assembly[] assemblys)
        {
            var config = services.GetService<common.user.Config>();
            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));
            Logger.Instance.Info("账号权限模块已加载");
            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));
        }

        public void LoadBefore(ServiceCollection services, Assembly[] assemblys)
        {
            services.AddSingleton<common.user.Config>();
            services.AddSingleton<IUserStore, UserStore>();
            services.AddSingleton<IUserMapInfoCaching, UserMapInfoCaching>();
        }
    }
}
