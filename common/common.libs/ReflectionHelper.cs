using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace common.libs
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class ReflectionHelper
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static IEnumerable<Type> GetInterfaceSchieves(Type type)
        {
            return GetInterfaceSchieves(AppDomain.CurrentDomain.GetAssemblies(), type);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="assemblys"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static IEnumerable<Type> GetInterfaceSchieves(Assembly[] assemblys, Type type)
        {
            return assemblys.SelectMany(c => c.GetTypes())
               .Where(c => !c.IsAbstract).Where(c => c.GetInterfaces().Contains(type));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="assemblys"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static IEnumerable<Type> GetSubClass(Assembly[] assemblys, Type type)
        {
            return assemblys.SelectMany(c => c.GetTypes())
               .Where(c => !c.IsAbstract).Where(c => c.IsSubclassOf(type));
        }
    }
}
