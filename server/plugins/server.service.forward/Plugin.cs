using Microsoft.Extensions.DependencyInjection;
using common.libs;
using common.server;
using System.Reflection;
using common.forward;
using server.service.forward;

namespace server.service.tcpforward
{
    public sealed class Plugin : IPlugin
    {
        public void LoadAfter(ServiceProvider services, Assembly[] assemblys)
        {
            services.GetService<ServerForwardProxyPlugin>();

            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));
            Logger.Instance.Info($"tcp转发和http1.1代理已加载");
            var config = services.GetService<common.forward.Config>();
            if (config.ConnectEnable)
            {
                Logger.Instance.Debug($"tcp转发和http1.1代理已允许注册");
            }
            else
            {
                Logger.Instance.Info($"tcp转发和http1.1代理未允许注册");
            }
            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));
        }

        public void LoadBefore(ServiceCollection services, Assembly[] assemblys)
        {
            services.AddSingleton<common.forward.Config>();//启动器

            services.AddSingleton<IForwardTargetCaching<ForwardTargetCacheInfo>, ForwardTargetCaching>();
            services.AddSingleton<IForwardUdpTargetCaching<ForwardTargetCacheInfo>, ForwardUdpTargetCaching>();


            services.AddSingleton<IForwardTargetProvider, ServerForwardTargetProvider>();
            services.AddSingleton<IForwardUdpTargetProvider, ServerForwardUdpTargetProvider>();
            services.AddSingleton<IForwardProxyPlugin, ForwardProxyPlugin>();

        }
    }
}
