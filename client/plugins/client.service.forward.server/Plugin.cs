using common.server;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace client.service.forward.server
{
    public sealed class Plugin : IPlugin
    {
        public void LoadAfter(ServiceProvider services, Assembly[] assemblys)
        {
            services.GetService<ServerForwardTransfer>();
        }

        public void LoadBefore(ServiceCollection services, Assembly[] assemblys)
        {
            services.AddSingleton<ServerForwardTransfer>();
            services.AddSingleton<ServerForwardMessengerSender>();
        }
    }
}
