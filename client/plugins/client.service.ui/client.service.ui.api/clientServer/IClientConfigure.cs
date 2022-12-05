using System.Threading.Tasks;

namespace client.service.ui.api.clientServer
{
    /// <summary>
    /// 前端修改配置接口
    /// </summary>
    public interface IClientConfigure
    {
        /// <summary>
        /// 名字
        /// </summary>
        string Name { get; }
        /// <summary>
        /// 作者
        /// </summary>
        string Author { get; }
        /// <summary>
        /// 描述
        /// </summary>
        string Desc { get; }
        /// <summary>
        /// 启用
        /// </summary>
        bool Enable { get; }
        /// <summary>
        /// 加载配置
        /// </summary>
        /// <returns></returns>
        Task<string> Load();
        /// <summary>
        /// 保存配置
        /// </summary>
        /// <param name="jsonStr"></param>
        /// <returns></returns>
        Task<string> Save(string jsonStr);
    }
}
