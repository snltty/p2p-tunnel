using common.libs;
using common.libs.extends;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Reflection;
using server.service.tcpforward;
using common.server.middleware;
using server.service.manager;
using System.Linq;
using common.socks5;
using server.service.socks5;
using server.service.udpforward;
using System.Threading;
using common.server;

namespace server.service
{
    class Program
    {
        static void Main(string[] args)
        {
            ThreadPool.SetMinThreads(150, 150);
            Logger.Instance.Info("正在启动...");
            LoggerConsole();

            //加载插件程序集，当单文件发布或者动态加载dll外部插件时需要，否则如果本程序集没有显式的使用它的相关内容的话，会加载不出来
            //可以改为从dll文件加载
            Assembly[] assemblys = new Assembly[] {
                typeof(CounterMessenger).Assembly,
                typeof(TcpForwardMessenger).Assembly,
                typeof(UdpForwardMessenger).Assembly,
                typeof(Socks5Messenger).Assembly,
                typeof(Socks5ClientHandler).Assembly,
            }.Concat(AppDomain.CurrentDomain.GetAssemblies()).ToArray();

            ServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddMiddleware(assemblys);
            IPlugin[] plugins = PluginLoader.LoadBefore(serviceCollection, assemblys);

            ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
            serviceProvider.UseMiddleware(assemblys);
            PluginLoader.LoadAfter(plugins, serviceProvider, assemblys);

            var config = serviceProvider.GetService<Config>();
            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));
            Logger.Instance.Info("没什么报红的，就说明运行成功了");
            Logger.Instance.Info($"UDP端口:{config.Udp}");
            Logger.Instance.Info($"TCP端口:{config.Tcp}");
            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));

            Console.ReadLine();
        }


        static void LoggerConsole()
        {
            Logger.Instance.OnLogger.Sub((model) =>
            {
                ConsoleColor currentForeColor = Console.ForegroundColor;
                switch (model.Type)
                {
                    case LoggerTypes.DEBUG:
                        Console.ForegroundColor = ConsoleColor.Blue;
                        break;
                    case LoggerTypes.INFO:
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    case LoggerTypes.WARNING:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    case LoggerTypes.ERROR:
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                    default:
                        break;
                }
                Console.WriteLine($"[{model.Type.ToString().PadRight(7)}][{model.Time:yyyy-MM-dd HH:mm:ss}]:{model.Content}");
                Console.ForegroundColor = currentForeColor;
            });
        }
    }
}
