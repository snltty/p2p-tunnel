using common.server;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace client.service.udpforward.server
{
    public sealed class Plugin : IPlugin
    {
        public void LoadAfter(ServiceProvider services, Assembly[] assemblys)
        {
            services.GetService<ServerUdpForwardTransfer>();
        }

        public void LoadBefore(ServiceCollection services, Assembly[] assemblys)
        {
            services.AddSingleton<ServerUdpForwardTransfer>();
        }
    }
}
