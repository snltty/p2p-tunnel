using common.server;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace client.service.httpProxy
{
    public sealed class Plugin : IPlugin
    {
        public void LoadAfter(ServiceProvider services, Assembly[] assemblys)
        {
            services.GetService<HttpProxyTransfer>();
        }

        public void LoadBefore(ServiceCollection services, Assembly[] assemblys)
        {
            services.AddSingleton<HttpProxyTransfer>();
        }
    }
}
