using common.libs;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using server.service.tcpforward;
using System.Linq;
using server.service.socks5;
using server.service.udpforward;
using System.Threading;
using common.server;
using System.IO;
using server.service.users;
using common.libs.database;

namespace server.service
{
    class Program
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (a, b) =>
            {
                Logger.Instance.Error(b.ExceptionObject + "");
            };


            ThreadPool.SetMinThreads(150, 150);
            Logger.Instance.Info("正在启动...");
            LoggerConsole();

            //加载插件程序集，当单文件发布或者动态加载dll外部插件时需要，否则如果本程序集没有显式的使用它的相关内容的话，会加载不出来
            //可以改为从dll文件加载
            Assembly[] assemblys = new Assembly[] {
                typeof(TcpForwardMessenger).Assembly,
                typeof(UdpForwardMessenger).Assembly,
                typeof(Socks5Messenger).Assembly,
                typeof(UsersMessenger).Assembly,
            }.Concat(AppDomain.CurrentDomain.GetAssemblies()).ToArray();

            ServiceCollection serviceCollection = new ServiceCollection();
            ServiceProvider serviceProvider = null;
            //注入 依赖注入服务供应 使得可以在别的地方通过注入的方式获得 ServiceProvider 以用来获取其它服务
            serviceCollection.AddSingleton((e) => serviceProvider);

            IPlugin[] plugins = PluginLoader.LoadBefore(serviceCollection, assemblys);

            serviceProvider = serviceCollection.BuildServiceProvider();
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
            if (Directory.Exists("log") == false)
            {
                Directory.CreateDirectory("log");
            }

            Logger.Instance.OnLogger += (model) =>
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

                string line = $"[{model.Type,-7}][{model.Time:yyyy-MM-dd HH:mm:ss}]:{model.Content}";
                Console.WriteLine(line);
                Console.ForegroundColor = currentForeColor;

                using StreamWriter sw = File.AppendText(Path.Combine("log", $"{DateTime.Now:yyyy-MM-dd}.log"));
                sw.WriteLine(line);
                sw.Flush();
                sw.Close();
                sw.Dispose();
            };
        }
    }
}
