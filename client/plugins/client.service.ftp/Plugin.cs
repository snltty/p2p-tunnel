using client.service.ftp.client;
using client.service.ftp.server;
using common.libs;
using common.server;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;

namespace client.service.ftp
{
    public class Plugin : IPlugin
    {
        public void LoadAfter(ServiceProvider services, Assembly[] assemblys)
        {
            services.GetService<FtpServer>().LoadPlugins(assemblys);
            services.GetService<FtpClient>().LoadPlugins(assemblys);
            Logger.Instance.Debug("文件服务插件已加载");
        }

        public void LoadBefore(ServiceCollection services, Assembly[] assemblys)
        {
            services.AddSingleton<Config>();

            foreach (var item in ReflectionHelper.GetInterfaceSchieves(assemblys, typeof(IFtpCommandServerPlugin)))
            {
                services.AddSingleton(item);
            }
            foreach (var item in ReflectionHelper.GetInterfaceSchieves(assemblys, typeof(IFtpCommandClientPlugin)))
            {
                services.AddSingleton(item);
            }
            services.AddSingleton<FtpServer>();
            services.AddSingleton<FtpClient>();
        }
    }
}
