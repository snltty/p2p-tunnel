using common.libs;
using common.libs.extends;
using Microsoft.Extensions.DependencyInjection;
using common.server.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using common.server;
using common.server.servers.iocp;
using common.server.servers.rudp;
using System.Reflection;
using server.service.messengers.register;
using server.messengers.register;
using common.libs.database;

namespace server.service
{
    public class Plugin : IPlugin
    {

        public void LoadBefore(ServiceCollection services, Assembly[] assemblys)
        {
            services.AddTransient(typeof(IConfigDataProvider<>), typeof(ConfigDataFileProvider<>));
            services.AddSingleton<Config>();
            services.AddSingleton<ITcpServer, TcpServer>();
            services.AddSingleton<IUdpServer, UdpServer>();

            services.AddSingleton<IClientRegisterCaching, ClientRegisterCaching>();
            services.AddSingleton<ISourceConnectionSelector, SourceConnectionSelector>();
            services.AddSingleton<IRegisterKeyValidator, DefaultRegisterKeyValidator>();
            services.AddSingleton<MessengerResolver>();
            services.AddSingleton<MessengerSender>();
            services.AddSingleton<ICryptoFactory, CryptoFactory>();
            services.AddSingleton<IAsymmetricCrypto, RsaCrypto>();
            services.AddSingleton<WheelTimer<object>>();

            foreach (Type item in ReflectionHelper.GetInterfaceSchieves(assemblys, typeof(IMessenger)))
            {
                services.AddSingleton(item);
            }
        }

        public void LoadAfter(ServiceProvider services, Assembly[] assemblys)
        {
            var config = services.GetService<Config>();

            var server = services.GetService<ITcpServer>();
            server.SetBufferSize(config.TcpBufferSize);
            server.Start(config.Tcp, ip: IPAddress.Any);
            Logger.Instance.Info("TCP服务已开启");

            services.GetService<IUdpServer>().Start(services.GetService<Config>().Udp);
            Logger.Instance.Info("UDP服务已开启");


            MessengerResolver messenger = services.GetService<MessengerResolver>();
            MessengerSender sender = services.GetService<MessengerSender>();
            foreach (Type item in ReflectionHelper.GetInterfaceSchieves(assemblys, typeof(IMessenger)))
            {
                messenger.LoadMessenger(item, services.GetService(item));
            }

            Loop(services);
        }

        private void Loop(ServiceProvider services)
        {
            IClientRegisterCaching clientRegisterCache = services.GetService<IClientRegisterCaching>();
            MessengerResolver messengerResolver = services.GetService<MessengerResolver>();
            MessengerSender messengerSender = services.GetService<MessengerSender>();

            clientRegisterCache.OnChanged.Sub((changeClient) =>
            {
                List<ClientsClientInfo> clients = clientRegisterCache.GetBySameGroup(changeClient.GroupId).Where(c => c.OnLineConnection != null && c.OnLineConnection.Connected).Select(c => new ClientsClientInfo
                {
                    Connection = c.OnLineConnection,
                    Id = c.Id,
                    Name = c.Name,
                    Mac = c.Mac,
                    Tcp = c.TcpConnection != null,
                    Udp = c.UdpConnection != null
                }).ToList();
                if (clients.Any())
                {
                    byte[] bytes = new ClientsInfo
                    {
                        Clients = clients.ToArray()
                    }.ToBytes();
                    foreach (ClientsClientInfo client in clients)
                    {
                        _ = messengerSender.SendOnly(new MessageRequestWrap
                        {
                            Connection = client.Connection,
                            Memory = bytes,
                            Path = "clients/execute"
                        }).ConfigureAwait(false);
                    }
                }
            });
        }
    }
}
