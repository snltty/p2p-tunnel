using client.messengers.clients;
using client.messengers.punchHole;
using client.messengers.punchHole.tcp;
using client.messengers.punchHole.udp;
using client.messengers.register;
using client.realize.messengers.clients;
using client.realize.messengers.crypto;
using client.realize.messengers.heart;
using client.realize.messengers.punchHole;
using client.realize.messengers.punchHole.tcp.nutssb;
using client.realize.messengers.punchHole.udp;
using client.realize.messengers.register;
using common.libs;
using Microsoft.Extensions.DependencyInjection;
using common.server;
using System;
using System.Linq;
using System.Reflection;
using common.libs.database;
using common.server.servers.rudp;
using common.server.servers.iocp;

namespace client.realize.messengers
{
    public class Plugin:IPlugin
    {
        public void LoadAfter(ServiceProvider services, Assembly[] assemblys)
        {
            services.GetService<IClientsTransfer>();
            services.GetService<MessengerSender>();
            MessengerResolver serverPluginHelper = services.GetService<MessengerResolver>();

            //加载所有的消息处理器
            foreach (Type item in ReflectionHelper.GetInterfaceSchieves(assemblys, typeof(IMessenger)))
            {
                serverPluginHelper.LoadMessenger(item, services.GetService(item));
            }
            //加载所有的打洞消息处理器
            services.GetService<PunchHoleMessengerSender>().LoadPlugins(assemblys);
        }

        public void LoadBefore(ServiceCollection services, Assembly[] assemblys)
        {
            services.AddSingleton<Config>();
            services.AddTransient(typeof(IConfigDataProvider<>), typeof(ConfigDataFileProvider<>));

            services.AddSingleton<ISourceConnectionSelector, SourceConnectionSelector>();

            //监听服务
            services.AddSingleton<ITcpServer, TcpServer>();
            services.AddSingleton<IUdpServer, UdpServer>();
            services.AddSingleton<MessengerResolver>();
            services.AddSingleton<MessengerSender>();

            //一些简单接口功能
            services.AddSingleton<HeartMessengerSender>();

            //客户端列表
            services.AddSingleton<IClientInfoCaching, ClientInfoCaching>();
            services.AddSingleton<IClientsTransfer, ClientsTransfer>();
            services.AddSingleton<ClientsMessengerSender>();

            //注册
            services.AddSingleton<RegisterMessengerSender>();
            services.AddSingleton<IRegisterTransfer, RegisterTransfer>();
            services.AddSingleton<RegisterStateInfo>();

            //打洞
            services.AddSingleton<PunchHoleMessengerSender>();
            //services.AddSingleton<IPunchHoleUdp, PunchHoleUdpMessengerSender>();
            services.AddSingleton<IPunchHoleUdp, PunchHoleRUdpMessengerSender>();
            services.AddSingleton<IPunchHoleTcp, PunchHoleTcpNutssBMessengerSender>();

            //默认时间轮
            services.AddSingleton<WheelTimer<object>>();

            //加密交换
            services.AddSingleton<ICryptoFactory, CryptoFactory>();
            services.AddSingleton<IAsymmetricCrypto, RsaCrypto>();
            services.AddSingleton<ISymmetricCrypto, AesCrypto>();
            services.AddSingleton<CryptoSwap>();

            //注入所有的消息处理器
            foreach (var item in ReflectionHelper.GetInterfaceSchieves(assemblys, typeof(IMessenger)))
            {
                services.AddSingleton(item);
            }

            //注入所有的打洞消息处理器
            foreach (Type item in ReflectionHelper.GetInterfaceSchieves(assemblys, typeof(IPunchHole)))
            {
                services.AddSingleton(item);
            }
        }
    }
}
