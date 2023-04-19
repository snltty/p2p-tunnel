using common.libs;
using common.server;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace client.service.logger
{
    public sealed class Plugin : IPlugin
    {
        public void LoadAfter(ServiceProvider services, Assembly[] assemblys)
        {
            LoggerClientService plugin = services.GetService<LoggerClientService>();
            Config config = services.GetService<Config>();
            Logger.Instance.OnLogger += (model) =>
            {
                if (config.Enable)
                {
                    plugin.Data.Add(model);
                    if (plugin.Data.Count > config.MaxLength)
                    {
                        plugin.Data.RemoveAt(0);
                    }
                }
            };

            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));
            Logger.Instance.Debug($"日志收集已加载");
            if (config.Enable)
            {
                Logger.Instance.Debug($"日志收集已启用：最长条数:{config.MaxLength}");
            }
            else
            {
                Logger.Instance.Info($"日志收集未启用");
            }
            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));
        }
        public void LoadBefore(ServiceCollection services, Assembly[] assemblys)
        {
            services.AddSingleton<Config>();
        }
    }

}
