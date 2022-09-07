using Microsoft.Extensions.DependencyInjection;
using common.libs;
using common.udpforward;
using common.server;
using System.Reflection;

namespace server.service.udpforward
{
    public class Plugin : IPlugin
    {
        public void LoadAfter(ServiceProvider services, Assembly[] assemblys)
        {
            services.GetService<UdpForwardTransfer>();
            services.GetService<UdpForwardResolver>();

            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));
            Logger.Instance.Info($"udp转发已加载");
            var config = services.GetService<common.udpforward.Config>();
            if (config.ConnectEnable)
            {
                Logger.Instance.Debug($"udp转发已允许注册");
            }
            else
            {
                Logger.Instance.Info($"udp转发未允许注册");
            }
            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));
        }

        public void LoadBefore(ServiceCollection services, Assembly[] assemblys)
        {
            services.AddSingleton<common.udpforward.Config>();//启动器
            services.AddSingleton<IUdpForwardServer, UdpForwardServer>(); //监听服务
            services.AddSingleton<UdpForwardMessengerSender>(); //消息发送
            services.AddSingleton<UdpForwardTransfer>();//启动器

            services.AddSingleton<IUdpForwardTargetProvider, UdpForwardTargetProvider>(); //目标提供器
            services.AddSingleton<IUdpForwardTargetCaching<UdpForwardTargetCacheInfo>, UdpForwardTargetCaching>(); //转发缓存器
            services.AddSingleton<UdpForwardResolver>();
        }
    }
}
