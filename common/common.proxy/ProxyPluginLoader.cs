using common.libs;
using System;
using System.Collections.Concurrent;

namespace common.proxy
{
    public interface IProxyPlugin
    {
        public byte Id { get; }
        public EnumProxyBufferSize BufferSize { get; }


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
        public bool HandleRequestData(ProxyInfo info);

        /// <summary>
        /// 验证权限，可以在这里阻止访问
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public bool ValidateAccess(ProxyInfo info);

        /// <summary>
        /// 回复数据预处理
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public void HandleAnswerData(ProxyInfo info);


        public void Started(ushort port)
        {

        }
        public void Stoped(ushort port)
        {

        }
    }

    public static class ProxyPluginLoader
    {
        delegate void DelegateValidateData(Memory<byte> data);
        delegate void DelegateValidateAccess(ProxyInfo info);
        delegate void DelegateCommandAnswer(ProxyInfo info);

        static ConcurrentDictionary<byte, IProxyPlugin> plugins = new ConcurrentDictionary<byte, IProxyPlugin>();
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
}
