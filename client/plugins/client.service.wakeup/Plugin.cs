using common.server;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace client.service.wakeup
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
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <param name="assemblys"></param>
        public void LoadBefore(ServiceCollection services, Assembly[] assemblys)
        {
            services.AddSingleton<Config>();
            services.AddSingleton<WakeUpTransfer>();
            services.AddSingleton<WakeUpMessengerSender>();
        }
    }
}
