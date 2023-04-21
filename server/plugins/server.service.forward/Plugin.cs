using Microsoft.Extensions.DependencyInjection;
using common.libs;
using common.server;
using System.Reflection;
using common.forward;
using common.proxy;

namespace server.service.forward
{
    public sealed class Plugin : IPlugin
    {
        public void LoadAfter(ServiceProvider services, Assembly[] assemblys)
        {
            ProxyPluginLoader.LoadPlugin(services.GetService<IForwardProxyPlugin>());

            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));
            Logger.Instance.Info($"端口转发穿透已加载");
            var config = services.GetService<common.forward.Config>();
            if (config.ConnectEnable)
            {
                Logger.Instance.Debug($"端口转发穿透已允许注册");
            }
            else
            {
                Logger.Instance.Info($"端口转发穿透未允许注册");
            }
            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));
        }

        public void LoadBefore(ServiceCollection services, Assembly[] assemblys)
        {
            services.AddSingleton<common.forward.Config>();//启动器

            services.AddSingleton<IForwardTargetCaching<ForwardTargetCacheInfo>, ForwardTargetCaching>();


            services.AddSingleton<IForwardTargetProvider, ForwardTargetProvider>();
            services.AddSingleton<IForwardProxyPlugin, ForwardProxyPlugin>();

        }
    }
}
