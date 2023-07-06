using common.libs;
using Microsoft.Extensions.DependencyInjection;
using System;
using common.server;
using common.server.servers.iocp;
using common.server.servers.rudp;
using System.Reflection;
using common.libs.database;
using server.service.validators;
using server.messengers.singnin;
using server.service.messengers.singnin;
using common.proxy;

namespace server.service
{
    public sealed class Plugin : IPlugin
    {
        public void LoadBefore(ServiceCollection services, Assembly[] assemblys)
        {
            services.AddTransient(typeof(IConfigDataProvider<>), typeof(ConfigDataFileProvider<>));
            services.AddSingleton<Config>();
            services.AddSingleton<ITcpServer, TcpServer>();
            services.AddSingleton<IUdpServer, UdpServer>();

            services.AddSingleton<IClientSignInCaching, ClientSignInCaching>();
            services.AddSingleton<IRelaySourceConnectionSelector, messengers.RelaySourceConnectionSelector>();


            services.AddSingleton<ISignInValidatorHandler, SignInValidatorHandler>();
            services.AddSingleton<IRelayValidator, RelayValidator>();
            services.AddSingleton<IServiceAccessValidator, validators.ServiceAccessValidator>();


            services.AddSingleton<MessengerResolver>();
            services.AddSingleton<MessengerSender>();
            services.AddSingleton<ICryptoFactory, CryptoFactory>();
            services.AddSingleton<IAsymmetricCrypto, RsaCrypto>();
            services.AddSingleton<WheelTimer<object>>();

            services.AddSingleton<common.proxy.Config>();
            services.AddSingleton<IProxyMessengerSender, ProxyMessengerSender>();
            services.AddSingleton<IProxyClient, ProxyClient>();
            services.AddSingleton<IProxyServer, ProxyServer>();
            services.AddSingleton<ProxyPluginValidatorHandler>();
            foreach (Type item in ReflectionHelper.GetInterfaceSchieves(assemblys, typeof(IProxyPluginValidator)))
            {
                services.AddSingleton(item);
            }


            foreach (Type item in ReflectionHelper.GetInterfaceSchieves(assemblys, typeof(IMessenger)))
            {
                services.AddSingleton(item);
            }
            foreach (Type item in ReflectionHelper.GetInterfaceSchieves(assemblys, typeof(ISignInValidator)))
            {
                services.AddSingleton(item);
            }
        }

        public void LoadAfter(ServiceProvider services, Assembly[] assemblys)
        {
            var config = services.GetService<Config>();

            var server = services.GetService<ITcpServer>();
            server.SetBufferSize((1 << (byte)config.TcpBufferSize) * 1024);
            try
            {
                server.Start(config.Tcp);
                Logger.Instance.Info("TCP服务已开启");
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }

            var udpServer = services.GetService<IUdpServer>();
            try
            {
                udpServer.Start(config.Udp, timeout: config.TimeoutDelay);
                Logger.Instance.Info("UDP服务已开启");
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }

            MessengerResolver messengerResolver = services.GetService<MessengerResolver>();
            messengerResolver.LoadMessenger(assemblys);

            ISignInValidatorHandler signInMiddlewareHandler = services.GetService<ISignInValidatorHandler>();
            signInMiddlewareHandler.LoadValidator(assemblys);

            ProxyPluginValidatorHandler proxyPluginValidatorHandler = services.GetService<ProxyPluginValidatorHandler>();
            proxyPluginValidatorHandler.LoadValidator(assemblys);
        }



    }
}
