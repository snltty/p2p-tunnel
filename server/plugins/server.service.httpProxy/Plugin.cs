using common.httpProxy;
using common.libs;
using common.proxy;
using common.server;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace server.service.httpProxy
{
    public sealed class Plugin : IPlugin
    {
        public void LoadAfter(ServiceProvider services, Assembly[] assemblys)
        {
            ProxyPluginLoader.LoadPlugin(services.GetService<IServerHttpProxyPlugin>());
            common.httpProxy.Config config = services.GetService<common.httpProxy.Config>();
           

            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));
            Logger.Instance.Debug($"http代理已加载，插件id:{config.Plugin}");
            if (config.ConnectEnable)
            {
                Logger.Instance.Debug($"http代理已允许连接");
            }
            else
            {
                Logger.Instance.Info($"http代理未允许连接");
            }
            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));
        }

        public void LoadBefore(ServiceCollection services, Assembly[] assemblys)
        {
            services.AddSingleton<common.httpProxy.Config>();
            services.AddSingleton<IServerHttpProxyPlugin, ServerHttpProxyPlugin>();
        }
    }
}
