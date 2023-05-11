using client.messengers.clients;
using client.messengers.punchHole;
using client.messengers.punchHole.tcp;
using client.messengers.punchHole.udp;
using client.messengers.singnin;
using client.realize.messengers.clients;
using client.realize.messengers.crypto;
using client.realize.messengers.heart;
using client.realize.messengers.punchHole;
using client.realize.messengers.punchHole.tcp.nutssb;
using client.realize.messengers.punchHole.udp;
using client.realize.messengers.singnin;
using common.libs;
using Microsoft.Extensions.DependencyInjection;
using common.server;
using System;
using System.Linq;
using System.Reflection;
using common.libs.database;
using common.server.servers.rudp;
using common.server.servers.iocp;
using client.realize.messengers.relay;
using client.messengers.relay;
using common.proxy;
using System.ComponentModel.DataAnnotations;

namespace client.realize.messengers
{
    public sealed class Plugin : IPlugin
    {
        public void LoadAfter(ServiceProvider services, Assembly[] assemblys)
        {
            services.GetService<IClientsTransfer>();
            services.GetService<MessengerSender>();
            MessengerResolver messengerResolver = services.GetService<MessengerResolver>();

            //加载所有的消息处理器
            messengerResolver.LoadMessenger(assemblys);
            //加载所有的打洞消息处理器
            services.GetService<PunchHoleMessengerSender>().LoadPlugins(assemblys);
        }

        public void LoadBefore(ServiceCollection services, Assembly[] assemblys)
        {
            services.AddSingleton<Config>();
            services.AddSingleton<PunchHoleDirectionConfig>();
            services.AddTransient(typeof(IConfigDataProvider<>), typeof(ConfigDataFileProvider<>));

            services.AddSingleton<IRelaySourceConnectionSelector, relay.RelaySourceConnectionSelector>();
            services.AddSingleton<IRelayValidator, RelayValidator>();
            services.AddSingleton<IClientConnectsCaching, ClientConnectsCaching>();


            services.AddSingleton<IIPv6AddressRequest, IPv6AddressRequest>();
            services.AddSingleton<IServiceAccessValidator, clients.ServiceAccessValidator>();

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
            services.AddSingleton<IClientsTunnel, ClientsTunnel>();
            services.AddSingleton<ClientsMessengerSender>();

            //注册
            services.AddSingleton<SignInMessengerSender>();
            services.AddSingleton<ISignInTransfer, SignInTransfer>();
            services.AddSingleton<SignInStateInfo>();

            //打洞
            services.AddSingleton<PunchHoleMessengerSender>();
            services.AddSingleton<IPunchHoleUdp, PunchHoleRUdpMessengerSender>();
            services.AddSingleton<IPunchHoleTcp, PunchHoleTcpNutssBMessengerSender>();

            services.AddSingleton<RelayMessengerSender>();

            //默认时间轮
            services.AddSingleton<WheelTimer<object>>();

            //加密交换
            services.AddSingleton<ICryptoFactory, CryptoFactory>();
            services.AddSingleton<IAsymmetricCrypto, RsaCrypto>();
            services.AddSingleton<ISymmetricCrypto, AesCrypto>();
            services.AddSingleton<CryptoSwap>();


            //代理
            services.AddSingleton<common.proxy.Config>();
            services.AddSingleton<IProxyMessengerSender, ProxyMessengerSender>();
            services.AddSingleton<IProxyClient, ProxyClient>();
            services.AddSingleton<IProxyServer, ProxyServer>();


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
