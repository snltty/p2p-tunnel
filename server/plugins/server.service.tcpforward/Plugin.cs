using Microsoft.Extensions.DependencyInjection;
using common.tcpforward;
using common.libs;
using common.server;
using System.Reflection;

namespace server.service.tcpforward
{
    public class Plugin : IPlugin
    {
        public void LoadAfter(ServiceProvider services, Assembly[] assemblys)
        {
            services.GetService<TcpForwardTransfer>();
            services.GetService<TcpForwardResolver>();

            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));
            Logger.Instance.Info($"tcp转发和http1.1代理已加载");
            var config = services.GetService<common.tcpforward.Config>();
            if (config.ConnectEnable)
            {
                Logger.Instance.Debug($"tcp转发和http1.1代理已允许注册");
            }
            else
            {
                Logger.Instance.Info($"tcp转发和http1.1代理未允许注册");
            }
            if (config.LanConnectEnable)
            {
                Logger.Instance.Debug($"tcp转发和http1.1代理已允许本地连接");
            }
            else
            {
                Logger.Instance.Info($"tcp转发和http1.1代理未允许未允许本地连接");
            }
            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));
        }

        public void LoadBefore(ServiceCollection services, Assembly[] assemblys)
        {
            services.AddSingleton<common.tcpforward.Config>();//启动器
            services.AddSingleton<ITcpForwardServer, TcpForwardServerPre>(); //监听服务
            services.AddSingleton<TcpForwardMessengerSender>(); //消息发送
            services.AddSingleton<TcpForwardTransfer>();//启动器

            services.AddSingleton<ITcpForwardTargetProvider, TcpForwardTargetProvider>(); //目标提供器
            services.AddSingleton<ITcpForwardTargetCaching<TcpForwardTargetCacheInfo>, TcpForwardTargetCaching>(); //转发缓存器
            services.AddSingleton<TcpForwardResolver>();
        }
    }
}
