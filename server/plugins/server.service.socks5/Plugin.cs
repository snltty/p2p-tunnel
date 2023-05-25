using common.libs;
using common.proxy;
using common.server;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace server.service.socks5
{
    public sealed class Plugin : IPlugin
    {
        public void LoadAfter(ServiceProvider services, Assembly[] assemblys)
        {
            ProxyPluginLoader.LoadPlugin(services.GetService<IServerSocks5ProxyPlugin>());

            common.socks5.Config config = services.GetService<common.socks5.Config>();
            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));
            Logger.Instance.Info($"socks5已加载，插件id:{config.Plugin}");
            if (config.ConnectEnable)
            {
                Logger.Instance.Debug($"socks5已允许连接");
            }
            else
            {
                Logger.Instance.Info($"socks5未允许连接");
            }
            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));
        }

        public void LoadBefore(ServiceCollection services, Assembly[] assemblys)
        {
            services.AddSingleton<common.socks5.Config>();
            services.AddSingleton<IServerSocks5ProxyPlugin, ServerSocks5ProxyPlugin>();
        }
    }

}
