using System.Collections.Generic;
using System.Reflection;

namespace client.service.ui.api.clientServer
{
    /// <summary>
    /// 前端接口服务
    /// </summary>
    public interface IClientServer
    {
        /// <summary>
        /// websocket
        /// </summary>
        public void Websocket();
        /// <summary>
        /// 具名插槽
        /// </summary>
        public void NamedPipe();
        /// <summary>
        /// 加载插件
        /// </summary>
        /// <param name="assemblys"></param>
        public void LoadPlugins(Assembly[] assemblys);
        /// <summary>
        /// 获取配置插件列表
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ClientServiceConfigureInfo> GetConfigures();
        /// <summary>
        /// 获取配置
        /// </summary>
        /// <param name="className"></param>
        /// <returns></returns>
        public IClientConfigure GetConfigure(string className);
        /// <summary>
        /// 获取配置名称列表
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetServices();

        /// <summary>
        /// 主动通知
        /// </summary>
        /// <param name="resp"></param>
        public void Notify(ClientServiceResponseInfo resp);
    }

    /// <summary>
    /// 前端配置服务信息
    /// </summary>
    public sealed class ClientServiceConfigureInfo
    {
        /// <summary>
        /// 名字
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// 类名
        /// </summary>
        public string ClassName { get; set; } = string.Empty;
        /// <summary>
        /// 作者
        /// </summary>
        public string Author { get; set; } = string.Empty;
        /// <summary>
        /// 描述
        /// </summary>
        public string Desc { get; set; } = string.Empty;
        /// <summary>
        /// 启用
        /// </summary>
        public bool Enable { get; set; } = false;
    }
}
