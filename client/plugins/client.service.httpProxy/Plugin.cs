using common.httpProxy;
using common.libs;
using common.proxy;
using common.server;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace client.service.httpProxy
{
    public sealed class Plugin : IPlugin
    {
        public void LoadAfter(ServiceProvider services, Assembly[] assemblys)
        {
            services.GetService<HttpProxyTransfer>();


            common.httpProxy.Config config = services.GetService<common.httpProxy.Config>();
            HttpProxyTransfer httpProxyTransfer = services.GetService<HttpProxyTransfer>();

            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));
            Logger.Instance.Debug($"http代理已加载");
            if (config.ListenEnable)
            {
                httpProxyTransfer.Update();
                Logger.Instance.Debug($"http代理已监听 127.0.0.1:{config.ListenPort}");
            }
            else
            {
                Logger.Instance.Info($"http代理未监听");
            }
            if (config.ConnectEnable)
            {
                Logger.Instance.Debug($"http代理已允许连接");
            }
            else
            {
                Logger.Instance.Info($"http代理未允许连接");
            }
            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));
        }

        public void LoadBefore(ServiceCollection services, Assembly[] assemblys)
        {
            services.AddSingleton<HttpProxyTransfer>();
            services.AddSingleton<common.httpProxy.Config>();
        }
    }
}
