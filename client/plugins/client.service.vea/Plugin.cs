using client.service.vea.socks5;
using common.libs;
using common.proxy;
using common.server;
using common.socks5;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace client.service.vea
{
    public sealed class Plugin : IPlugin
    {
        public void LoadAfter(ServiceProvider services, Assembly[] assemblys)
        {
            var transfer = services.GetService<VeaTransfer>();
            ProxyPluginLoader.LoadPlugin(services.GetService<IVeaSocks5ProxyPlugin>());

            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));
            Logger.Instance.Debug("vea 虚拟网卡插件已加载");
            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));

            if (services.GetService<Config>().Enable)
            {
                transfer.Run();
            }
        }

        public void LoadBefore(ServiceCollection services, Assembly[] assemblys)
        {
            services.AddSingleton<Config>();
            services.AddSingleton<VeaTransfer>();
            services.AddSingleton<VeaMessengerSender>();

            services.AddSingleton<IVeaSocks5ConnectionProvider, VeaSocks5ConnectionProvider>();
            services.AddSingleton<IVeaSocks5ProxyPlugin, VeaSocks5ProxyPlugin>();

        }
    }
}
