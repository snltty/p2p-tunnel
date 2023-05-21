using client.messengers.singnin;
using client.realize.messengers.punchHole;
using client.service.forward;
using client.service.forward.server;
using client.service.httpProxy;
using client.service.httpProxy.server;
using client.service.logger;
using client.service.proxy;
using client.service.socks5;
using client.service.socks5.server;
using client.service.ui.api.service.clientServer;
using client.service.users.server;
using client.service.vea;
using common.libs;
using common.proxy;
using common.server;
using common.user;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;

namespace client.service
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Startup.Start();

            await Helper.Await();
        }
    }

    class Startup
    {
        public static void Start()
        {
            Console.WriteLine($"GCSettings.IsServerGC:{GCSettings.IsServerGC}");

            AppDomain.CurrentDomain.UnhandledException += (a, b) =>
            {
                Logger.Instance.Error(b.ExceptionObject + "");
            };


            ThreadPool.SetMinThreads(1024, 1024);
            ThreadPool.SetMaxThreads(65535, 65535);
            LoggerConsole();
            Logger.Instance.Info("正在启动...");

            //加载插件程序集，当单文件发布或者动态加载dll外部插件时需要，否则如果本程序集没有显式的使用它的相关内容的话，会加载不出来
            //可以改为从dll文件加载
            Assembly[] assemblys = new Assembly[] {
                typeof(ClientServer).Assembly,
                typeof(LoggerClientService).Assembly,
                typeof(PunchHoleMessenger).Assembly,

                typeof(ProxyMessenger).Assembly,

                typeof(ForwardClientService).Assembly,
                typeof(ServerForwardClientService).Assembly,

                typeof(HttpProxyClientService).Assembly,
                typeof(ServerHttpProxyClientService).Assembly,

                typeof(Socks5ClientService).Assembly,
                typeof(ServerSocks5ClientService).Assembly,

                typeof(VeaClientService).Assembly,

                typeof(UsersClientService).Assembly,
                typeof(ServerUsersClientService).Assembly,

                typeof(ProxyClientService).Assembly,

                //以下是为了获取信息
                typeof(common.server.model.SignInMessengerIds).Assembly,
                typeof(ProxyMessengerIds).Assembly,
                typeof(common.httpProxy.HttpProxyMessengerIds).Assembly,
                typeof(common.socks5.Socks5MessengerIds).Assembly,
                typeof(common.forward.ForwardMessengerIds).Assembly,

                typeof(UsersMessengerIds).Assembly,

            }.Concat(AppDomain.CurrentDomain.GetAssemblies()).ToArray();

            ServiceCollection serviceCollection = new ServiceCollection();
            ServiceProvider serviceProvider = null;
            //注入 依赖注入服务供应 使得可以在别的地方通过注入的方式获得 ServiceProvider 以用来获取其它服务
            serviceCollection.AddSingleton((e) => serviceProvider);

            IPlugin[] plugins = PluginLoader.LoadBefore(serviceCollection, assemblys);

            serviceProvider = serviceCollection.BuildServiceProvider();
            PluginLoader.LoadAfter(plugins, serviceProvider, assemblys);

            PrintProxyPlugin(serviceProvider, assemblys);

            SignInStateInfo signInStateInfo = serviceProvider.GetService<SignInStateInfo>();
            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));
            Logger.Instance.Info("获取外网距离ing...");
            signInStateInfo.LocalInfo.RouteLevel = NetworkHelper.GetRouteLevel(out List<IPAddress> ips);
            signInStateInfo.LocalInfo.RouteIps = ips.ToArray();
            Logger.Instance.Info($"外网距离:{signInStateInfo.LocalInfo.RouteLevel}");
            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));

            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));
            Logger.Instance.Warning("没什么报红的，就说明运行成功了");
            Logger.Instance.Warning($"当前版本：{Helper.Version}，如果与服务器版本不一致，则可能无法连接");
            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));

            Config config = serviceProvider.GetService<Config>();
            //自动注册
            if (config.Client.AutoReg)
            {
                serviceProvider.GetService<ISignInTransfer>().SignIn(config.Client.AutoReg);
            }
        }

        private static void LoggerConsole()
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

                 if (model.Type >= LoggerTypes.WARNING)
                 {
                     using StreamWriter sw = File.AppendText(Path.Combine("log", $"{DateTime.Now:yyyy-MM-dd}.log"));
                     sw.WriteLine(line);
                     sw.Flush();
                     sw.Close();
                     sw.Dispose();
                 }
             };
        }

        private static void PrintProxyPlugin(ServiceProvider services, Assembly[] assemblys)
        {
            var iAccesss = ReflectionHelper.GetInterfaceSchieves(assemblys, typeof(IAccess)).Distinct()
                .Select(c => services.GetService(c)).Concat(ProxyPluginLoader.plugins.Values).Where(c => c is IAccess).Select(c => (IAccess)c);

            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));
            Logger.Instance.Debug("权限值,uint 每个权限占一位，最多32个权限");
            Logger.Instance.Info($"{Convert.ToString((uint)EnumServiceAccess.Relay, 2).PadLeft(Logger.Instance.PaddingWidth, '0')}  relay");
            foreach (var item in iAccesss.OrderBy(c => c.Access))
            {
                Logger.Instance.Info($"{Convert.ToString(item.Access, 2).PadLeft(Logger.Instance.PaddingWidth, '0')}  {item.Name}");
            }
            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));
        }
    }

}
