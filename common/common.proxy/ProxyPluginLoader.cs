using common.libs;
using common.server;
using common.server.model;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;

namespace common.proxy
{
    public interface IProxyPlugin : IAccess
    {
        public byte Id { get; }
        public EnumBufferSize BufferSize { get; }
        public IPAddress BroadcastBind { get; }


        /// <summary>
        /// 验证数据完整性
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public EnumProxyValidateDataResult ValidateData(ProxyInfo info);

        /// <summary>
        /// 请求数据预处理
        /// </summary>
        /// <param name="info"></param>
        /// <returns>是否发送给目标端</returns>
        public bool HandleRequestData(ProxyInfo info);

        /// <summary>
        /// 验证权限，可以在这里阻止访问
        /// </summary>
        /// <param name="info"></param>
        /// <returns>是否通过验证</returns>
        public bool ValidateAccess(ProxyInfo info);

        /// <summary>
        /// 回复数据预处理
        /// </summary>
        /// <param name="info"></param>
        /// <returns>是否发送给来源端</returns>
        public bool HandleAnswerData(ProxyInfo info);


        public void Started(ushort port) { }
        public void Stoped(ushort port) { }
    }
    public interface IProxyPluginValidator
    {
        bool Validate(ProxyInfo info);
    }

    public static class ProxyPluginLoader
    {
        delegate void DelegateValidateData(Memory<byte> data);
        delegate void DelegateValidateAccess(ProxyInfo info);
        delegate void DelegateCommandAnswer(ProxyInfo info);

        public static ConcurrentDictionary<byte, IProxyPlugin> plugins = new ConcurrentDictionary<byte, IProxyPlugin>();
        public static void LoadPlugin(IProxyPlugin plugin)
        {
            if (plugins.ContainsKey(plugin.Id))
            {
                Logger.Instance.Error($"plugin {plugin.Id} : {plugin.GetType().Name} already exists");
            }
            else
            {
                plugins.TryAdd(plugin.Id, plugin);
            }
        }
        public static bool GetPlugin(byte id, out IProxyPlugin plugin)
        {
            return plugins.TryGetValue(id, out plugin);
        }

    }

    public sealed class ProxyPluginValidatorHandler
    {
        Wrap<IProxyPluginValidator> first;
        Wrap<IProxyPluginValidator> last;

        private readonly ServiceProvider serviceProvider;
        public ProxyPluginValidatorHandler(ServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public void LoadValidator(Assembly[] assemblys)
        {
            foreach (IProxyPluginValidator validator in ReflectionHelper.GetInterfaceSchieves(assemblys, typeof(IProxyPluginValidator)).Distinct().Select(c => (IProxyPluginValidator)serviceProvider.GetService(c)))
            {
                if (first == null)
                {
                    first = new Wrap<IProxyPluginValidator> { Value = validator };
                    last = first;
                }
                else
                {
                    last.Next = new Wrap<IProxyPluginValidator> { Value = validator };
                    last = last.Next;
                }
            }
        }
        public bool Validate(ProxyInfo info)
        {
            Wrap<IProxyPluginValidator> current = first;
            while (current != null)
            {
                if (current.Value.Validate(info) == false)
                {
                    return false;
                }
                current = current.Next;
            }
            return true;
        }
    }


    sealed class Wrap<T>
    {
        public T Value { get; set; }
        public Wrap<T> Next { get; set; }
    }
}
