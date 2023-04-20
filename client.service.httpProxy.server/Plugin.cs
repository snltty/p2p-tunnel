using common.server;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace client.service.httpProxy.server
{
    public sealed class Plugin : IPlugin
    {
        public void LoadAfter(ServiceProvider services, Assembly[] assemblys)
        {
        }

        public void LoadBefore(ServiceCollection services, Assembly[] assemblys)
        {
        }
    }

}
