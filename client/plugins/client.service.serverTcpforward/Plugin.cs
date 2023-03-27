using common.server;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace client.service.tcpforward.server
{
    public sealed class Plugin : IPlugin
    {
        public void LoadAfter(ServiceProvider services, Assembly[] assemblys)
        {
            services.GetService<ServerTcpForwardTransfer>();
        }

        public void LoadBefore(ServiceCollection services, Assembly[] assemblys)
        {
            services.AddSingleton<ServerTcpForwardTransfer>();
        }
    }
}
