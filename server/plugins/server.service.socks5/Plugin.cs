using common.libs;
using common.server;
using common.socks5;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace server.service.socks5
{
    public class Plugin : IPlugin
    {
        public void LoadAfter(ServiceProvider services, Assembly[] assemblys)
        {
            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));
            Logger.Instance.Info("socks5已加载");
            Config config = services.GetService<Config>();
            if (config.ConnectEnable)
            {
                Logger.Instance.Debug($"socks5已允许连接");
            }
            else
            {
                Logger.Instance.Info($"socks5未允许连接");
            }
            if (config.LanConnectEnable)
            {
                Logger.Instance.Debug($"socks5已允许本地连接");
            }
            else
            {
                Logger.Instance.Info($"socks5未允许本地连接");
            }
            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));
        }

        public void LoadBefore(ServiceCollection services, Assembly[] assemblys)
        {
            services.AddSingleton<Config>();

            services.AddSingleton<ISocks5ClientListener, Socks5ClientListener>();
            services.AddSingleton<ISocks5MessengerSender, Socks5MessengerSender>();

            services.AddSingleton<ISocks5ServerHandler, Socks5ServerHandler>();
            services.AddSingleton<ISocks5ClientHandler, Socks5ClientHandler>();
        }
    }

}
