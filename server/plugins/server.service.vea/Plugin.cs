using Microsoft.Extensions.DependencyInjection;
using common.server;
using System.Reflection;
using common.libs;
using common.proxy;
using common.vea;

namespace server.service.vea
{
    public sealed class Plugin : IPlugin
    {
        public void LoadAfter(ServiceProvider services, Assembly[] assemblys)
        {
            ProxyPluginLoader.LoadPlugin(services.GetService<IVeaSocks5ProxyPlugin>());
            var config = services.GetService<common.vea.Config>();
            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));
            Logger.Instance.Info("组网自动分配IP模块已加载");
            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));
        }

        public void LoadBefore(ServiceCollection services, Assembly[] assemblys)
        {
            services.AddSingleton<common.vea.Config>();
            services.AddSingleton<IVeaAccessValidator, VeaSocks5ProxyPlugin>();
            services.AddSingleton<IVeaSocks5ProxyPlugin, VeaSocks5ProxyPlugin>();
        }
    }
}
