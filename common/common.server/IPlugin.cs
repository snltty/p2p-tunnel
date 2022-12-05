using common.libs;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace common.server
{
    /// <summary>
    /// 
    /// </summary>
    public interface IPlugin
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <param name="assemblys"></param>
        void LoadBefore(ServiceCollection services, Assembly[] assemblys);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <param name="assemblys"></param>
        void LoadAfter(ServiceProvider services, Assembly[] assemblys);
    }

    /// <summary>
    /// 
    /// </summary>
    public static class PluginLoader
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <param name="assemblys"></param>
        /// <returns></returns>
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="plugins"></param>
        /// <param name="services"></param>
        /// <param name="assemblys"></param>
        public static void LoadAfter(IPlugin[] plugins, ServiceProvider services, Assembly[] assemblys)
        {
            foreach (var item in plugins)
            {
                item.LoadAfter(services, assemblys);
            }
        }
    }
}
