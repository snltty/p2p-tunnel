using Microsoft.Extensions.DependencyInjection;
using common.server;
using System.Reflection;
using common.user;

namespace client.service.users
{
    public sealed class Plugin : IPlugin
    {
        public void LoadAfter(ServiceProvider services, Assembly[] assemblys)
        {
        }

        public void LoadBefore(ServiceCollection services, Assembly[] assemblys)
        {
            services.AddSingleton<IUserMapInfoCaching, UserMapInfoCaching>();

        }
    }
}
