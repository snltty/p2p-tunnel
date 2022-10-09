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
using System.Runtime.Intrinsics.Arm;
using System.Threading;
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

        private const byte TryReverseMinValue = 1;
        private const byte TryReverseMaxValue = 2;

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


            punchHoleUdp.OnStep1Handler.Sub((e) => { clientInfoCaching.Connecting(e.RawData.FromId, true, ServerType.UDP); });
            punchHoleUdp.OnStep2FailHandler.Sub((e) =>
            {
                clientInfoCaching.Connecting(e.RawData.FromId, false, ServerType.UDP);
                if (e.RawData.TunnelName > (ulong)TunnelDefaults.MAX)
                {
                    clientInfoCaching.RemoveTunnelPort(e.RawData.TunnelName);
                    clientInfoCaching.RemoveUdpserver(e.RawData.TunnelName);
                    _ = clientsMessengerSender.RemoveTunnel(registerState.OnlineConnection, e.RawData.TunnelName);
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

            punchHoleTcp.OnStep1Handler.Sub((e) => clientInfoCaching.Connecting(e.RawData.FromId, true, ServerType.TCP));
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
        public void ConnectClient(ClientInfo info)
        {
            ConnectClient(info, TryReverseMinValue);
        }
        public void ConnectClient(ClientInfo info, byte tryreverse)
        {
            if (info.Id == registerState.ConnectId)
            {
                return;
            }
            Task.Run(async () =>
            {
                bool anyFail = false, anySuccess = false;
                if (config.Client.UseUdp && info.Udp)
                {
                    if (info.UdpConnecting == false && info.UdpConnected == false)
                    {
                        bool udp = await ConnectUdp(info, (ulong)TunnelDefaults.UDP, registerState.LocalInfo.UdpPort).ConfigureAwait(false);
                        if (udp == false)
                        {
                            udp = await ConnectUdp(info, (ulong)TunnelDefaults.MIN, registerState.LocalInfo.UdpPort).ConfigureAwait(false);
                        }
                        anyFail &= udp;
                        anySuccess |= udp;
                    }
                    else
                    {
                        anyFail &= info.UdpConnected;
                        anySuccess |= info.UdpConnected;
                    }
                }
                if (config.Client.UseTcp && info.Tcp)
                {
                    if (info.TcpConnecting == false && info.TcpConnected == false)
                    {
                        bool tcp = await ConnectTcp(info, (ulong)TunnelDefaults.TCP, registerState.LocalInfo.TcpPort).ConfigureAwait(false);
                        if (tcp == false)
                        {
                            tcp = await ConnectTcp(info, (ulong)TunnelDefaults.MIN, registerState.LocalInfo.TcpPort).ConfigureAwait(false);
                        }
                        anyFail &= tcp;
                        anySuccess |= tcp;
                    }
                    else
                    {
                        anyFail &= info.TcpConnected;
                        anySuccess |= info.TcpConnected;
                    }
                }

                //有未成功的，并且尝试次数没达到限制，就通知对方反向连接
                if (anyFail == false && tryreverse < TryReverseMaxValue)
                {
                    ConnectReverse(info.Id, tryreverse);
                    return;
                }
                //一个都没成功的，才中继，任一成功则不中继
                if (anySuccess == false)
                {
                    Relay(info, ServerType.UDP, true);
                    Relay(info, ServerType.TCP, true);
                }
            });
        }

        public void ConnectReverse(ulong id)
        {
            ConnectReverse(id, TryReverseMinValue);
        }
        private void ConnectReverse(ulong id, byte tryreverse)
        {
            punchHoleMessengerSender.SendReverse(id, tryreverse).ConfigureAwait(false);
        }
        private void OnReverse(OnPunchHoleArg arg)
        {
            if (clientInfoCaching.Get(arg.Data.FromId, out ClientInfo client))
            {
                PunchHoleReverseInfo model = new PunchHoleReverseInfo();
                model.DeBytes(arg.Data.Data);
                ConnectClient(client, (byte)(model.TryReverse + 1));
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

        private async Task<bool> ConnectUdp(ClientInfo info, ulong tunnelName, int localPort)
        {
            if (info.UdpConnected) return info.UdpConnected;

            clientInfoCaching.Connecting(info.Id, true, ServerType.UDP);
            var result = await punchHoleUdp.Send(new ConnectParams
            {
                Id = info.Id,
                TunnelName = tunnelName,
                TryTimes = 2,
                LocalPort = localPort
            }).ConfigureAwait(false);
            if (result.State)
            {
                return result.State;
            }

            Logger.Instance.Error((result.Result as ConnectFailModel).Msg);
            clientInfoCaching.Offline(info.Id, ServerType.UDP);
            return false;
        }
        private async Task<bool> ConnectTcp(ClientInfo info, ulong tunnelName, int localPort)
        {
            if (info.TcpConnected) return info.TcpConnected;

            clientInfoCaching.Connecting(info.Id, true, ServerType.TCP);
            var result = await punchHoleTcp.Send(new ConnectParams
            {
                Id = info.Id,
                TunnelName = tunnelName,
                TryTimes = 2,
                LocalPort = localPort
            }).ConfigureAwait(false);
            if (result.State)
            {
                return result.State;
            }
            Logger.Instance.Error((result.Result as ConnectFailModel).Msg);
            clientInfoCaching.Offline(info.Id, ServerType.TCP);
            return false;
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

                foreach (ClientsClientInfo item in upLineClients)
                {
                    ClientInfo client = new ClientInfo
                    {
                        Id = item.Id,
                        Name = item.Name,
                        Mac = item.Mac,
                        Tcp = item.Tcp,
                        Udp = item.Udp,
                        AutoPunchHole = item.AutoPunchHole,
                    };
                    clientInfoCaching.Add(client);
                    if (firstClients.Get())
                    {
                        if (config.Client.AutoPunchHole && client.AutoPunchHole)
                        {
                            if (registerState.LocalInfo.TcpPort == registerState.RemoteInfo.TcpPort || registerState.LocalInfo.UdpPort == registerState.RemoteInfo.UdpPort)
                            {
                                ConnectClient(client);
                            }
                            else
                            {
                                ConnectReverse(client.Id);
                            }
                        }
                        else if (registerState.RemoteInfo.Relay)
                        {
                            Relay(client, ServerType.UDP, true);
                            Relay(client, ServerType.TCP, true);
                        }
                    }
                }

                firstClients.Reverse();
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
        }
        private void Relay(ClientInfo client, ServerType serverType, bool notify)
        {
            if (registerState.RemoteInfo.Relay)
            {
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
        }
    }
}
