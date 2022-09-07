using client.service.ui.api.clientServer;
using client.service.ui.api.service.clientServer;
using client.service.ui.api.service.webServer;
using common.libs;
using common.server;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Reflection;

namespace client.service.ui.api.service
{
    public class Plugin : IPlugin
    {
        public void LoadAfter(ServiceProvider services, Assembly[] assemblys)
        {
            LoadWebAfter(services, assemblys);
            LoadApiAfter(services, assemblys);
        }

        public void LoadBefore(ServiceCollection services, Assembly[] assemblys)
        {
            services.AddSingleton<Config>();
            LoadWebBefore(services, assemblys);
            LoadApiBefore(services, assemblys);
        }

        private void LoadWebBefore(ServiceCollection services, Assembly[] assemblys)
        {
            services.AddSingleton<IWebServer, WebServer>();
            services.AddSingleton<IWebServerFileReader, WebServerFileReader>();
        }
        private void LoadWebAfter(ServiceProvider services, Assembly[] assemblys)
        {
            var config = services.GetService<Config>();

            if (config.EnableWeb)
            {
                services.GetService<IWebServer>().Start();
                Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));
                Logger.Instance.Debug("管理UI，web已启用");
                Logger.Instance.Info($"管理UI web1 :http://{config.Web.BindIp}:{config.Web.Port}");
                Logger.Instance.Info($"管理UI web2 :https://snltty.gitee.io/p2p-tunnel");
                Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));
            }
            else
            {
                Logger.Instance.Debug("管理UI，web未启用");
            }
        }

        private void LoadApiBefore(ServiceCollection services, Assembly[] assemblys)
        {
            services.AddSingleton<IClientServer, ClientServer>();

            IEnumerable<Type> types = assemblys.SelectMany(c => c.GetTypes());

            foreach (var item in types.Where(c => c.GetInterfaces().Contains(typeof(IClientService))))
            {
                services.AddSingleton(item);
            }
            foreach (var item in types.Where(c => c.GetInterfaces().Contains(typeof(IClientConfigure))))
            {
                services.AddSingleton(item);
            }
        }
        private void LoadApiAfter(ServiceProvider services, Assembly[] assemblys)
        {
            IClientServer clientServer = services.GetService<IClientServer>();

            var config = services.GetService<Config>();

            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));
            if (config.EnableWeb)
            {
                clientServer.Websocket();
                Logger.Instance.Debug($"管理UI，websocket已启用:{config.Websocket.BindIp}:{config.Websocket.Port}");
            }
            else
            {
                Logger.Instance.Info($"管理UI，websocket未启用");
            }
            if (config.EnableCommand)
            {
                clientServer.NamedPipe();
                Logger.Instance.Debug($"管理UI，命令行已启用");
            }
            else
            {
                Logger.Instance.Info($"管理UI，命令行未启用");
            }

            if (config.EnableApi)
            {
                clientServer.LoadPlugins(assemblys);
                Logger.Instance.Debug($"管理UI，api已启用");
            }
            else
            {
                Logger.Instance.Info($"管理UI，api未启用");
            }
            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));
        }

    }
}
