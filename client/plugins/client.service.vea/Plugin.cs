using common.libs;
using common.server;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace client.service.vea
{
    public class Plugin : IPlugin
    {
        public void LoadAfter(ServiceProvider services, Assembly[] assemblys)
        {
            var transfer = services.GetService<VirtualEthernetAdapterTransfer>();
            services.GetService<IVeaSocks5ClientHandler>();

            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));
            Logger.Instance.Debug("vea 虚拟网卡插件已加载");
            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));

            transfer.Run();

        }

        public void LoadBefore(ServiceCollection services, Assembly[] assemblys)
        {
            services.AddSingleton<Config>();
            services.AddSingleton<VirtualEthernetAdapterTransfer>();
            services.AddSingleton<VeaMessengerSender>();
            services.AddSingleton<IVeaSocks5ClientHandler, VeaSocks5ClientHandler>();
            services.AddSingleton<IVeaSocks5ServerHandler, VeaSocks5ServerHandler>();
            services.AddSingleton<IVeaSocks5ClientListener, VeaSocks5ClientListener>();
            services.AddSingleton<IVeaSocks5MessengerSender, VeaSocks5MessengerSender>();
        }
    }
}
