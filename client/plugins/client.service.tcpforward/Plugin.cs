using common.libs;
using common.server;
using common.tcpforward;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace client.service.tcpforward
{
    public class Plugin : IPlugin
    {
        public void LoadAfter(ServiceProvider services, Assembly[] assemblys)
        {
            services.GetService<TcpForwardTransfer>();
            services.GetService<TcpForwardResolver>();

            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));
            Logger.Instance.Debug("tcp转发、http1.1代理已加载");
            Logger.Instance.Debug("tcp转发、http1.1代理已启动");
            var config = services.GetService<common.tcpforward.Config>();

            if (config.ConnectEnable)
            {
                Logger.Instance.Debug($"tcp转发已允许连接");
            }
            else
            {
                Logger.Instance.Info($"tcp转发未允许连接");
            }
            if (config.LanConnectEnable)
            {
                Logger.Instance.Debug($"http1.1代理已允许本地连接");
            }
            else
            {
                Logger.Instance.Info($"http1.1代理未允许本地连接");
            }
            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));
        }

        public void LoadBefore(ServiceCollection services, Assembly[] assemblys)
        {
            services.AddSingleton<common.tcpforward.Config>();

            services.AddSingleton<ITcpForwardServer, TcpForwardServerPre>();
            services.AddSingleton<ITcpForwardTargetProvider, TcpForwardTargetProvider>();
            services.AddSingleton<ITcpForwardTargetCaching<TcpForwardTargetCacheInfo>, TcpForwardTargetCaching>();

            services.AddSingleton<TcpForwardTransfer>();
            services.AddSingleton<TcpForwardResolver>();
            services.AddSingleton<TcpForwardMessengerSender>();
        }
    }
}
