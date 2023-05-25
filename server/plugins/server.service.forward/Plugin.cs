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
            IProxyServer proxyServer = services.GetService<IProxyServer>();

            common.forward.Config config = services.GetService<common.forward.Config>();
            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));
            Logger.Instance.Info($"端口转发穿透已加载，插件id:{config.Plugin}");
            if (config.ConnectEnable)
            {
                Logger.Instance.Debug($"端口转发穿透已允许注册");
            }
            else
            {
                Logger.Instance.Info($"端口转发穿透未允许注册");
            }

            Logger.Instance.Info("端口转发穿透服务已启动...");
            foreach (ushort port in config.WebListens)
            {
                proxyServer.Start(port, config.Plugin, (byte)ForwardAliveTypes.Web);
                Logger.Instance.Warning($"端口转发穿透监听:{port}");
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
