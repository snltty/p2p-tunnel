using System.Threading.Tasks;

namespace client.service.ui.api.clientServer
{
    public interface IClientConfigure
    {
        string Name { get; }
        string Author { get; }
        string Desc { get; }
        bool Enable { get; }
        Task<bool> SwitchEnable(bool enable);
        Task<object> Load();
        Task<string> Save(string jsonStr);
    }
}
