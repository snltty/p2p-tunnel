using common.libs;
using common.server;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Runtime.InteropServices;

namespace client.service.hijack
{
    public sealed class Plugin : IPlugin
    {
        public void LoadAfter(ServiceProvider services, Assembly[] assemblys)
        {
            Config config = services.GetService<Config>();

            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));
            Logger.Instance.Debug($"劫持代理插件已加载");
            if (config.ListenEnable)
            {
                Logger.Instance.Debug($"劫持代理插件已开启");
            }
            else
            {
                Logger.Instance.Info($"劫持代理插件未开启");
            }
            if (config.ConnectEnable)
            {
                Logger.Instance.Debug($"劫持代理插件已允许连接");
            }
            else
            {
                Logger.Instance.Info($"劫持代理插件未允许连接");
            }
            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));
        }

        public void LoadBefore(ServiceCollection services, Assembly[] assemblys)
        {
            services.AddSingleton<Config>();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                services.AddSingleton<IHijackPlatform, HijackWindows>();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                services.AddSingleton<IHijackPlatform, HijackMacOs>();
            }
            else
            {
                services.AddSingleton<IHijackPlatform, HijackLinux>();
            }

        }
    }
}
