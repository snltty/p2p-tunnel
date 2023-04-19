using common.forward;
using common.libs;
using common.server;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace client.service.forward
{
    public sealed class Plugin : IPlugin
    {
        public void LoadAfter(ServiceProvider services, Assembly[] assemblys)
        {
            services.GetService<ForwardTransfer>();

            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));
            Logger.Instance.Debug("端口转发已加载");
            var config = services.GetService<common.forward.Config>();
            if (config.ConnectEnable)
            {
                Logger.Instance.Debug($"端口转发已允许连接");
            }
            else
            {
                Logger.Instance.Info($"端口转发未允许连接");
            }
            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));
        }

        public void LoadBefore(ServiceCollection services, Assembly[] assemblys)
        {
            services.AddSingleton<common.forward.Config>();

            services.AddSingleton<IForwardTargetCaching<ForwardTargetCacheInfo>, ForwardTargetCaching>();
            services.AddSingleton<IForwardUdpTargetCaching<ForwardTargetCacheInfo>, ForwardUdpTargetCaching>();
            services.AddSingleton<ForwardTransfer>();


            services.AddSingleton<IForwardTargetProvider, ForwardTargetProvider>();
            services.AddSingleton<IForwardUdpTargetProvider, ForwardUdpTargetProvider>();
            services.AddSingleton<IForwardProxyPlugin, ForwardProxyPlugin>();


        }
    }
}
