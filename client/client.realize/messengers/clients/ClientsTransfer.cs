using client.messengers.clients;
using client.messengers.punchHole;
using client.messengers.punchHole.tcp;
using client.messengers.punchHole.udp;
using client.messengers.register;
using client.realize.messengers.punchHole;
using common.libs;
using common.server;
using common.server.model;
using common.server.servers.rudp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace client.realize.messengers.clients
{
    public class ClientsTransfer : IClientsTransfer
    {
        private BoolSpace firstClients = new BoolSpace(true);

        private readonly IPunchHoleUdp punchHoleUdp;
        private readonly IPunchHoleTcp punchHoleTcp;
        private readonly RegisterStateInfo registerState;
        private readonly IClientInfoCaching clientInfoCaching;
        private readonly PunchHoleMessengerSender punchHoleMessengerSender;
        private readonly Config config;
        private readonly IUdpServer udpServer;

        private const byte TryReverseMaxValue = 2;
        private object lockObject = new();

        public ClientsTransfer(ClientsMessengerSender clientsMessengerSender,
            IPunchHoleUdp punchHoleUdp, IPunchHoleTcp punchHoleTcp, IClientInfoCaching clientInfoCaching,
            RegisterStateInfo registerState, PunchHoleMessengerSender punchHoleMessengerSender, Config config,
            IUdpServer udpServer, ITcpServer tcpServer
        )
        {
            this.punchHoleUdp = punchHoleUdp;
            this.punchHoleTcp = punchHoleTcp;
            this.registerState = registerState;
            this.clientInfoCaching = clientInfoCaching;
            this.config = config;
            this.udpServer = udpServer;


            punchHoleUdp.OnStep1Handler.Sub((e) =>
            {
                if (config.Client.UseUdp == true)
                {
                    clientInfoCaching.Connecting(e.RawData.FromId, true, ServerType.UDP);
                }
            });
            punchHoleUdp.OnStep2FailHandler.Sub((e) =>
            {
                if (clientInfoCaching.Get(e.RawData.FromId, out ClientInfo client))
                {
                    clientInfoCaching.Connecting(e.RawData.FromId, false, ServerType.UDP);
                    if (e.RawData.TunnelName > (ulong)TunnelDefaults.MAX)
                    {
                        clientInfoCaching.RemoveTunnelPort(e.RawData.TunnelName);
                        clientInfoCaching.RemoveUdpserver(e.RawData.TunnelName);
                        _ = clientsMessengerSender.RemoveTunnel(registerState.OnlineConnection, e.RawData.TunnelName);
                    }
                }

            });
            punchHoleUdp.OnStep3Handler.Sub((e) =>
            {
                clientInfoCaching.Online(e.Data.FromId, e.Connection, ClientConnectTypes.P2P);
                if (e.RawData.TunnelName > (ulong)TunnelDefaults.MAX)
                {
                    clientInfoCaching.RemoveTunnelPort(e.RawData.TunnelName);
                    clientInfoCaching.RemoveUdpserver(e.RawData.TunnelName);
                    _ = clientsMessengerSender.RemoveTunnel(registerState.OnlineConnection, e.RawData.TunnelName);
                }
            });
            punchHoleUdp.OnStep4Handler.Sub((e) =>
            {
                clientInfoCaching.Online(e.Data.FromId, e.Connection, ClientConnectTypes.P2P);
                if (e.RawData.TunnelName > (ulong)TunnelDefaults.MAX)
                {
                    clientInfoCaching.RemoveTunnelPort(e.RawData.TunnelName);
                    clientInfoCaching.RemoveUdpserver(e.RawData.TunnelName);
                    _ = clientsMessengerSender.RemoveTunnel(registerState.OnlineConnection, e.RawData.TunnelName);
                }
            });

            punchHoleTcp.OnStep1Handler.Sub((e) =>
            {
                if (config.Client.UseTcp == true)
                {
                    clientInfoCaching.Connecting(e.RawData.FromId, true, ServerType.TCP);
                }
            });
            punchHoleTcp.OnStep2FailHandler.Sub((e) =>
            {
                clientInfoCaching.Connecting(e.RawData.FromId, false, ServerType.TCP);
                if (e.RawData.TunnelName > (ulong)TunnelDefaults.MAX)
                {
                    clientInfoCaching.RemoveTunnelPort(e.RawData.TunnelName);
                    _ = clientsMessengerSender.RemoveTunnel(registerState.OnlineConnection, e.RawData.TunnelName);
                }
            });
            punchHoleTcp.OnStep3Handler.Sub((e) =>
            {
                clientInfoCaching.Online(e.Data.FromId, e.Connection, ClientConnectTypes.P2P);
                if (e.RawData.TunnelName > (ulong)TunnelDefaults.MAX)
                {
                    clientInfoCaching.RemoveTunnelPort(e.RawData.TunnelName);
                    _ = clientsMessengerSender.RemoveTunnel(registerState.OnlineConnection, e.RawData.TunnelName);

                }
            });
            punchHoleTcp.OnStep4Handler.Sub((e) =>
            {
                clientInfoCaching.Online(e.Data.FromId, e.Connection, ClientConnectTypes.P2P);
                if (e.RawData.TunnelName > (ulong)TunnelDefaults.MAX)
                {
                    clientInfoCaching.RemoveTunnelPort(e.RawData.TunnelName);
                    _ = clientsMessengerSender.RemoveTunnel(registerState.OnlineConnection, e.RawData.TunnelName);
                }
            });

            //调试注释
            tcpServer.OnDisconnect.Sub((connection) => Disconnect(connection, registerState.TcpConnection));
            udpServer.OnDisconnect.Sub((connection) => Disconnect(connection, registerState.UdpConnection));

            //中继连线
            punchHoleMessengerSender.OnRelay.Sub((param) =>
            {
                if (clientInfoCaching.Get(param.Raw.Data.FromId, out ClientInfo client))
                {
                    Relay(client, param.Relay.ServerType, false);
                }
            });

            this.punchHoleMessengerSender = punchHoleMessengerSender;
            //有人要求反向链接
            punchHoleMessengerSender.OnReverse.Sub(OnReverse);
            registerState.OnRegisterBind.Sub(OnRegisterBind);
            //收到来自服务器的 在线客户端 数据
            clientsMessengerSender.OnServerClientsData.Sub(OnServerSendClients);

            Logger.Instance.Info("获取外网距离ing...");
            registerState.LocalInfo.RouteLevel = NetworkHelper.GetRouteLevel();
        }

        private void Disconnect(IConnection connection, IConnection regConnection)
        {
            if (ReferenceEquals(regConnection, connection))
            {
                return;
            }
            clientInfoCaching.Offline(connection.ConnectId, connection.ServerType);
        }

        public void ConnectClient(ulong id)
        {
            if (clientInfoCaching.Get(id, out ClientInfo client))
            {
                ConnectClient(client);
            }
        }
        public void ConnectClient(ClientInfo client)
        {
            if (client.Id == registerState.ConnectId)
            {
                return;
            }
            Task.Run(async () =>
            {
                EnumConnectResult udp = await ConnectUdp(client).ConfigureAwait(false);
                EnumConnectResult tcp = await ConnectTcp(client).ConfigureAwait(false);

                EnumConnectResult result = udp | tcp;

                if (client.TryReverseValue < TryReverseMaxValue)
                {
                    if (result != EnumConnectResult.All)
                    {
                        client.TryReverseValue++;
                        ConnectReverse(client);
                    }
                }
                else
                {
                    if (result == EnumConnectResult.AllFail)
                    {
                        //Relay(client, ServerType.UDP, true);
                        Relay(client, ServerType.TCP, true);
                    }
                }

                client.TryReverseValue = 1;
            });
        }

        public void ConnectReverse(ulong id)
        {
            if (clientInfoCaching.Get(id, out ClientInfo client))
            {
                ConnectReverse(client);
            }
        }
        private void ConnectReverse(ClientInfo info)
        {
            punchHoleMessengerSender.SendReverse(info).ConfigureAwait(false);
        }
        private void OnReverse(OnPunchHoleArg arg)
        {
            if (clientInfoCaching.Get(arg.Data.FromId, out ClientInfo client))
            {
                PunchHoleReverseInfo model = new PunchHoleReverseInfo();
                model.DeBytes(arg.Data.Data);
                client.TryReverseValue = model.TryReverse;
                ConnectClient(client);
            }
        }

        public void Reset(ulong id)
        {
            punchHoleMessengerSender.SendReset(id).ConfigureAwait(false);

        }
        public void ConnectStop(ulong id)
        {
            punchHoleTcp.SendStep2Stop(id);
        }

        private async Task<EnumConnectResult> ConnectUdp(ClientInfo client)
        {
            if (client.UdpConnected)
            {
                return EnumConnectResult.UdpOnly;
            }
            if ((config.Client.UseUdp & client.UseUdp) == false)
            {
                return EnumConnectResult.AllFail;
            }
            if (client.TcpConnecting)
            {
                return EnumConnectResult.AllFail;
            }

            clientInfoCaching.Connecting(client.Id, true, ServerType.UDP);

            ulong[] tunnelNames = new ulong[] { (ulong)TunnelDefaults.UDP, (ulong)TunnelDefaults.MIN };
            for (int i = 0; i < tunnelNames.Length; i++)
            {
                ConnectResultModel result = await punchHoleUdp.Send(new ConnectParams
                {
                    Id = client.Id,
                    TunnelName = tunnelNames[i],
                    TryTimes = 2,
                    LocalPort = registerState.LocalInfo.UdpPort
                }).ConfigureAwait(false);
                if (result.State)
                {
                    return EnumConnectResult.UdpOnly;
                }
                Logger.Instance.Error((result.Result as ConnectFailModel).Msg);
            }

            clientInfoCaching.Offline(client.Id, ServerType.UDP);
            return EnumConnectResult.AllFail;
        }
        private async Task<EnumConnectResult> ConnectTcp(ClientInfo client)
        {
            if (client.TcpConnected)
            {
                return EnumConnectResult.TcpOnly;
            }
            if ((config.Client.UseTcp & client.UseTcp) == false)
            {
                return EnumConnectResult.AllFail;
            }
            if (client.TcpConnecting)
            {
                return EnumConnectResult.AllFail;
            }

            clientInfoCaching.Connecting(client.Id, true, ServerType.TCP);

            ulong[] tunnelNames = new ulong[] { (ulong)TunnelDefaults.TCP, (ulong)TunnelDefaults.MIN };
            for (int i = 0; i < tunnelNames.Length; i++)
            {
                ConnectResultModel result = await punchHoleTcp.Send(new ConnectParams
                {
                    Id = client.Id,
                    TunnelName = tunnelNames[i],
                    TryTimes = 2,
                    LocalPort = registerState.LocalInfo.UdpPort
                }).ConfigureAwait(false);
                if (result.State)
                {
                    return EnumConnectResult.TcpOnly;
                }
                Logger.Instance.Error((result.Result as ConnectFailModel).Msg);
            }

            clientInfoCaching.Offline(client.Id, ServerType.TCP);
            return EnumConnectResult.AllFail;
        }

        private void OnRegisterBind(bool state)
        {
            firstClients.Reset();
            clientInfoCaching.Clear();
            if (state)
            {
                clientInfoCaching.AddTunnelPort((ulong)TunnelDefaults.UDP, registerState.LocalInfo.UdpPort);
                clientInfoCaching.AddUdpserver((ulong)TunnelDefaults.UDP, udpServer as UdpServer);
                clientInfoCaching.AddTunnelPort((ulong)TunnelDefaults.TCP, registerState.LocalInfo.TcpPort);
            }
            else
            {
                clientInfoCaching.RemoveTunnelPort((ulong)TunnelDefaults.UDP);
                clientInfoCaching.RemoveUdpserver((ulong)TunnelDefaults.UDP);
                clientInfoCaching.RemoveTunnelPort((ulong)TunnelDefaults.TCP);
            }
        }

        private void OnServerSendClients(ClientsInfo clients)
        {
            try
            {
                if (registerState.OnlineConnection == null || clients.Clients == null)
                {
                    return;
                }
                lock (lockObject)
                {
                    IEnumerable<ulong> remoteIds = clients.Clients.Select(c => c.Id);
                    //下线了的
                    IEnumerable<ulong> offlines = clientInfoCaching.AllIds().Except(remoteIds).Where(c => c != registerState.ConnectId);
                    foreach (ulong offid in offlines)
                    {
                        clientInfoCaching.Offline(offid);
                        clientInfoCaching.Remove(offid);
                    }
                    //新上线的
                    IEnumerable<ulong> upLines = remoteIds.Except(clientInfoCaching.AllIds());
                    IEnumerable<ClientsClientInfo> upLineClients = clients.Clients.Where(c => upLines.Contains(c.Id) && c.Id != registerState.ConnectId);

                    bool first = firstClients.Get();
                    foreach (ClientsClientInfo item in upLineClients)
                    {
                        ClientInfo client = new ClientInfo
                        {
                            Id = item.Id,
                            Name = item.Name,
                            Mac = item.Mac,
                            UseTcp = item.Tcp,
                            UseUdp = item.Udp,
                            AutoPunchHole = item.AutoPunchHole,
                        };
                        clientInfoCaching.Add(client);
                        if (first)
                        {
                            if (config.Client.AutoPunchHole && client.AutoPunchHole)
                            {
                                if (registerState.LocalInfo.TcpPort == registerState.RemoteInfo.TcpPort || registerState.LocalInfo.UdpPort == registerState.RemoteInfo.UdpPort)
                                {
                                    ConnectClient(client);
                                }
                                else
                                {
                                    ConnectReverse(client);
                                }
                            }
                            else if (registerState.RemoteInfo.Relay)
                            {
                                //Relay(client, ServerType.UDP, true);
                                Relay(client, ServerType.TCP, true);
                            }
                        }
                    }

                    firstClients.Reverse();
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
        }
        private void Relay(ClientInfo client, ServerType serverType, bool notify)
        {
            if (registerState.RemoteInfo.Relay == false) return;

            if (client.TcpConnected == true && serverType == ServerType.TCP) return;
            if (client.UdpConnected == true && serverType == ServerType.UDP) return;

            IConnection connection = serverType switch
            {
                ServerType.TCP => registerState.TcpConnection?.Clone(),
                ServerType.UDP => registerState.UdpConnection?.Clone(),
                _ => throw new NotImplementedException(),
            };
            if (connection != null)
            {
                connection.Relay = registerState.RemoteInfo.Relay;
                clientInfoCaching.Online(client.Id, connection, ClientConnectTypes.Relay);
            }

            if (notify)
            {
                _ = punchHoleMessengerSender.SendRelay(client.Id, serverType);
            }
        }

        [Flags]
        enum EnumConnectResult : byte
        {
            AllFail = 0,
            UdpOnly = 1,
            TcpOnly = 2,
            All = UdpOnly | TcpOnly,
        }
    }
}
