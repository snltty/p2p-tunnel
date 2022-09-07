using common.libs;
using common.server;
using common.socks5;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace client.service.socks5
{
    public class Plugin : IPlugin
    {
        public void LoadAfter(ServiceProvider services, Assembly[] assemblys)
        {
            common.socks5.Config config = services.GetService<common.socks5.Config>();
            Socks5Transfer socks5Transfer = services.GetService<Socks5Transfer>();

            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));
            Logger.Instance.Debug($"socks5已加载");
            if (config.ListenEnable)
            {
                services.GetService<ISocks5ClientListener>().Start(config.ListenPort, config.BufferSize);
                if (config.IsPac)
                {
                    socks5Transfer.UpdatePac(new PacSetParamsInfo { IsCustom = config.IsCustomPac });
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
            services.AddSingleton<Socks5Transfer>();
            services.AddSingleton<common.socks5.Config>();
            services.AddSingleton<ISocks5ClientListener, Socks5ClientListener>();
            services.AddSingleton<ISocks5MessengerSender, Socks5MessengerSender>();

            services.AddSingleton<ISocks5ServerHandler, Socks5ServerHandler>();
            services.AddSingleton<ISocks5ClientHandler, Socks5ClientHandler>();
        }
    }

}
