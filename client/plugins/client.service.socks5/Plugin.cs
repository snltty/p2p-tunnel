using common.libs;
using common.proxy;
using common.server;
using common.socks5;
using Microsoft.Extensions.DependencyInjection;
using server.service.socks5;
using System.Reflection;

namespace client.service.socks5
{
    public sealed class Plugin : IPlugin
    {
        public void LoadAfter(ServiceProvider services, Assembly[] assemblys)
        {
            ProxyPluginLoader.LoadPlugin(services.GetService<IClientSocks5ProxyPlugin>());
            common.socks5.Config config = services.GetService<common.socks5.Config>();
            Socks5Transfer socks5Transfer = services.GetService<Socks5Transfer>();
           

            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));
            Logger.Instance.Debug($"socks5已加载");
            if (config.ListenEnable)
            {
                services.GetService<IProxyServer>().Start((ushort)config.ListenPort, config.Plugin);
                if (config.IsPac)
                {
                    socks5Transfer.UpdatePac();
                }
                Logger.Instance.Debug($"socks5已监听 socks5://127.0.0.1:{config.ListenPort}");
            }
            else
            {
                Logger.Instance.Info($"socks5未监听");
            }
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
            services.AddSingleton<Socks5Transfer>();
            services.AddSingleton<common.socks5.Config>();
            services.AddSingleton<IClientSocks5ProxyPlugin, ClientSocks5ProxyPlugin>();
        }
    }

}
