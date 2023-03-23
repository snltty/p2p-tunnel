﻿using common.libs;
using Microsoft.Extensions.DependencyInjection;
using common.server.model;
using System;
using System.Collections.Generic;
using System.Linq;
using common.server;
using common.server.servers.iocp;
using common.server.servers.rudp;
using System.Reflection;
using common.libs.database;
using server.messengers;
using server.service.validators;
using System.Net;
using server.service.messengers;
using common.libs.extends;
using server.messengers.singnin;
using server.service.messengers.singnin;

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

            services.AddSingleton<ISignInValidator, SignInValidator>();
            services.AddSingleton<SignInMiddlewareHandler>();
            services.AddSingleton<SignInEndTimeMiddleware>();
            services.AddSingleton<SignInNetFlowMiddleware>();
            services.AddSingleton<SignInSignLimitMiddleware>();
            services.AddSingleton<IServiceAccessValidator, ServiceAccessValidator>();
            services.AddSingleton<IRelayValidator, RelayValidator>();
            services.AddSingleton<IUserStore, UserStore>();


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
            services.GetService<ISignInValidator>();

            var server = services.GetService<ITcpServer>();
            server.SetBufferSize(config.TcpBufferSize);
            server.Start(config.Tcp);
            Logger.Instance.Info("TCP服务已开启");

            var udpServer = services.GetService<IUdpServer>();
            udpServer.Start(config.Udp, timeout: config.TimeoutDelay);
            Logger.Instance.Info("UDP服务已开启");

            MessengerResolver messenger = services.GetService<MessengerResolver>();
            foreach (Type item in ReflectionHelper.GetInterfaceSchieves(assemblys, typeof(IMessenger)).Distinct())
            {
                messenger.LoadMessenger(item, services.GetService(item));
            }

            //启用一些账号验证，想修改的话，可以参照 server.service.validators.SignInValidator 里的任一实现了 SignInMiddleware的类
            //比如我想把账号的过期时间单独处理，而不是吧数据保存在用户信息里，或者想加入一些别的验证
            SignInMiddlewareHandler signInMiddlewareHandler = services.GetService<SignInMiddlewareHandler>();
            signInMiddlewareHandler
                .Use(services.GetService<SignInEndTimeMiddleware>())
                .Use(services.GetService<SignInNetFlowMiddleware>())
                .Use(services.GetService<SignInSignLimitMiddleware>());

            Loop(services);
            Udp((UdpServer)udpServer, messenger);
        }

        private void Loop(ServiceProvider services)
        {
            IClientSignInCaching clientRegisterCache = services.GetService<IClientSignInCaching>();
            MessengerResolver messengerResolver = services.GetService<MessengerResolver>();
            MessengerSender messengerSender = services.GetService<MessengerSender>();

            clientRegisterCache.OnChanged.Sub((changeClient) =>
            {
                List<ClientsClientInfo> clients = clientRegisterCache.Get(changeClient.GroupId).Where(c => c.Connection != null && c.Connection.Connected).OrderBy(c => c.ConnectionId).Select(c => new ClientsClientInfo
                {
                    Connection = c.Connection,
                    Id = c.ConnectionId,
                    Name = c.Name,
                    Access = c.ClientAccess,
                }).ToList();

                if (clients.Any())
                {
                    byte[] bytes = new ClientsInfo
                    {
                        Clients = clients.ToArray()
                    }.ToBytes();
                    foreach (ClientsClientInfo client in clients)
                    {
                        messengerSender.SendOnly(new MessageRequestWrap
                        {
                            Connection = client.Connection,
                            Payload = bytes,
                            MessengerId = (ushort)ClientsMessengerIds.Notify
                        }).Wait();
                    }
                }
            });
        }

        ClientsMessenger clientsMessenger;
        private void Udp(UdpServer udpServer, MessengerResolver messenger)
        {
            if (messenger.GetMessenger((ushort)ClientsMessengerIds.AddTunnel, out object obj))
            {
                clientsMessenger = obj as ClientsMessenger;
            }
            udpServer.OnMessage += (IPEndPoint remoteEndpoint, Memory<byte> data) =>
            {
                try
                {
                    TunnelRegisterInfo model = new TunnelRegisterInfo();
                    model.DeBytes(data);
                    if (clientsMessenger != null)
                    {
                        clientsMessenger.AddTunnel(model, remoteEndpoint.Port);
                        udpServer.SendUnconnectedMessage(((ushort)remoteEndpoint.Port).ToBytes(), remoteEndpoint);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.DebugError(ex);
                }
            };
        }
    }
}
