using System.Collections.Generic;
using System.Reflection;

namespace client.service.ui.api.clientServer
{
    public interface IClientServer
    {
        public void Websocket();
        public void NamedPipe();
        public void LoadPlugins(Assembly[] assemblys);
        public IEnumerable<ClientServiceConfigureInfo> GetConfigures();
        public IClientConfigure GetConfigure(string className);
        public IEnumerable<string> GetServices();

        public void Notify(ClientServiceResponseInfo resp);
    }

    public class ClientServiceConfigureInfo
    {
        public string Name { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Desc { get; set; } = string.Empty;
        public bool Enable { get; set; } = false;
    }
}
