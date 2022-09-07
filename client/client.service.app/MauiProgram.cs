using client.messengers.register;
using client.realize.messengers.punchHole;
using client.service.logger;
using client.service.socks5;
using client.service.tcpforward;
using client.service.udpforward;
using client.service.ui.api.manager;
using client.service.ui.api.service.clientServer;
using client.service.ui.api.service.webServer;
using common.libs;
using common.libs.database;
using common.libs.extends;
using common.server;
using common.server.middleware;
using common.socks5;
using System.Net.Sockets;
using System.Net;
using System.Reflection;

namespace client.service.app
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            ThreadPool.SetMinThreads(150, 150);

            var builder = MauiApp.CreateBuilder();
            builder.UseMauiApp<App>();
            //builder.Services.AddMauiBlazorWebView();

            MauiApp app = builder.Build();

            return app;
        }
    }

    public class Startup
    {
        static ServiceProvider serviceProvider = null;
        public static void Start()
        {
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Loopback, 59411);
            UdpClient udp = new UdpClient();
            Logger.Instance.OnLogger.Sub((logger) =>
            {
                try
                {
                    udp.Send(logger.ToJson().ToBytes(), endpoint);
                }
                catch (Exception)
                {
                }
            });

            Logger.Instance.PaddingWidth = 10;
            Logger.Instance.Info("正在启动...");


            ServiceCollection serviceCollection = new ServiceCollection();
            //注入 依赖注入服务供应 使得可以在别的地方通过注入的方式获得 ServiceProvider 以用来获取其它服务
            serviceCollection.AddSingleton((e) => serviceProvider);


            //加载插件程序集，当单文件发布或者动态加载dll外部插件时需要，否则如果本程序集没有显式的使用它的相关内容的话，会加载不出来
            //可以改为从dll文件加载
            Assembly[] assemblys = new Assembly[] {
                typeof(LoggerClientService).Assembly,
                typeof(TcpForwardMessenger).Assembly,
                typeof(UdpForwardMessenger).Assembly,
                typeof(ClientServer).Assembly,
                typeof(CounterClientService).Assembly,
                typeof(Socks5ClientService).Assembly,
                typeof(Socks5Messenger).Assembly,
                typeof(PunchHoleMessenger).Assembly,
            }.Concat(AppDomain.CurrentDomain.GetAssemblies()).ToArray();


            serviceCollection.AddMiddleware(assemblys);
            IPlugin[] plugins = PluginLoader.LoadBefore(serviceCollection, assemblys);

            //覆盖几个实现，由于平台实现不一样
            serviceCollection.AddTransient(typeof(IConfigDataProvider<>), typeof(ConfigDataFileProvider<>));
            serviceCollection.AddSingleton<IWebServerFileReader, WebServerFileReader>();

            serviceProvider = serviceCollection.BuildServiceProvider();
            serviceProvider.UseMiddleware(assemblys);
            PluginLoader.LoadAfter(plugins, serviceProvider, assemblys);

            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));
            Logger.Instance.Warning("没什么报红的，就说明运行成功了");
            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));

            //自动注册
            if (serviceProvider.GetService<Config>().Client.AutoReg)
            {
                serviceProvider.GetService<IRegisterTransfer>().Register();
            }
        }
    }
}