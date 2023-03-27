using common.server;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace client.service.socks5.server
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
