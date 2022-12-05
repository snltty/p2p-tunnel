using common.libs;
using common.server;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace client.service.logger
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class Plugin : IPlugin
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <param name="assemblys"></param>
        public void LoadAfter(ServiceProvider services, Assembly[] assemblys)
        {
            LoggerClientService plugin = services.GetService<LoggerClientService>();
            Config config = services.GetService<Config>();
            Logger.Instance.OnLogger.Sub((model) =>
            {
                if (config.Enable)
                {
                    plugin.Data.Add(model);
                    if (plugin.Data.Count > config.MaxLength)
                    {
                        plugin.Data.RemoveAt(0);
                    }
                }
            });

            Logger.Instance.Debug("日志收集插件已加载");
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <param name="assemblys"></param>
        public void LoadBefore(ServiceCollection services, Assembly[] assemblys)
        {
            services.AddSingleton<Config>();
        }
    }

}
