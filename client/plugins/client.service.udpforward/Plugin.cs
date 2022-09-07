using common.libs;
using common.server;
using common.udpforward;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace client.service.udpforward
{
    public class Plugin : IPlugin
    {
        public void LoadAfter(ServiceProvider services, Assembly[] assemblys)
        {
            services.GetService<UdpForwardTransfer>();
            services.GetService<UdpForwardResolver>();

            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));
            Logger.Instance.Debug("Udp转发已加载");
            Logger.Instance.Debug("Udp转发已启动");
            var config = services.GetService<common.udpforward.Config>();

            if (config.ConnectEnable)
            {
                Logger.Instance.Debug($"udp转发已允许连接");
            }
            else
            {
                Logger.Instance.Info($"udp转发未允许连接");
            }
            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));
        }

        public void LoadBefore(ServiceCollection services, Assembly[] assemblys)
        {
            services.AddSingleton<common.udpforward.Config>();

            services.AddSingleton<IUdpForwardServer, UdpForwardServer>();
            services.AddSingleton<IUdpForwardTargetProvider, UdpForwardTargetProvider>();
            services.AddSingleton<IUdpForwardTargetCaching<UdpForwardTargetCacheInfo>, UdpForwardTargetCaching>();

            services.AddSingleton<UdpForwardTransfer>();
            services.AddSingleton<UdpForwardResolver>();
            services.AddSingleton<UdpForwardMessengerSender>();
        }
    }
}
