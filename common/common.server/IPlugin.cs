using common.libs;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace common.server
{
    public interface IPlugin
    {
        void LoadBefore(ServiceCollection services, Assembly[] assemblys);
        void LoadAfter(ServiceProvider services, Assembly[] assemblys);
    }

    public static class PluginLoader
    {
        public static IPlugin[] LoadBefore(ServiceCollection services, Assembly[] assemblys)
        {
            IEnumerable<Type> types = ReflectionHelper.GetInterfaceSchieves(assemblys, typeof(IPlugin)).Distinct();
            IPlugin[] plugins = types.Select(c => (IPlugin)Activator.CreateInstance(c)).ToArray();

            foreach (var item in plugins)
            {
                item.LoadBefore(services, assemblys);
            }

            return plugins;
        }

        public static void LoadAfter(IPlugin[] plugins, ServiceProvider services, Assembly[] assemblys)
        {
            foreach (var item in plugins)
            {
                item.LoadAfter(services, assemblys);
            }
        }
    }
}
