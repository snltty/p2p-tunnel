using client.messengers.clients;
using client.messengers.punchHole;
using client.messengers.punchHole.tcp;
using client.messengers.punchHole.udp;
using client.messengers.register;
using client.realize.messengers.punchHole;
using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using common.server.servers.iocp;
using common.server.servers.rudp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using client.realize.messengers.register;

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
        private readonly ITcpServer tcpServer;
        private readonly RegisterMessengerSender registerMessengerSender;

        private const byte TryReverseMinValue = 1;
        private const byte TryReverseMaxValue = 2;

        public ClientsTransfer(ClientsMessengerSender clientsMessengerSender,
            IPunchHoleUdp punchHoleUdp, IPunchHoleTcp punchHoleTcp, IClientInfoCaching clientInfoCaching,
            RegisterStateInfo registerState, PunchHoleMessengerSender punchHoleMessengerSender, Config config,
            IUdpServer udpServer, ITcpServer tcpServer, RegisterMessengerSender registerMessengerSender
        )
        {
            this.punchHoleUdp = punchHoleUdp;
            this.punchHoleTcp = punchHoleTcp;
            this.registerState = registerState;
            this.clientInfoCaching = clientInfoCaching;
            this.config = config;
            this.udpServer = udpServer;
            this.tcpServer = tcpServer;

            this.registerMessengerSender = registerMessengerSender;

            punchHoleUdp.OnStep1Handler.Sub((e) => clientInfoCaching.Connecting(e.RawData.FromId, true, ServerType.UDP));
            punchHoleUdp.OnStep2FailHandler.Sub((e) => clientInfoCaching.Connecting(e.RawData.FromId, false, ServerType.UDP));
            punchHoleUdp.OnStep3Handler.Sub((e) => { clientInfoCaching.Online(e.Data.FromId, e.Connection, ClientConnectTypes.P2P); });
            punchHoleUdp.OnStep4Handler.Sub((e) => { clientInfoCaching.Online(e.Data.FromId, e.Connection, ClientConnectTypes.P2P); });

            punchHoleTcp.OnStep1Handler.Sub((e) => clientInfoCaching.Connecting(e.RawData.FromId, true, ServerType.TCP));
            punchHoleTcp.OnStep2FailHandler.Sub((e) => clientInfoCaching.Connecting(e.RawData.FromId, false, ServerType.TCP));
            punchHoleTcp.OnStep3Handler.Sub((e) => clientInfoCaching.Online(e.Data.FromId, e.Connection, ClientConnectTypes.P2P));
            punchHoleTcp.OnStep4Handler.Sub((e) => clientInfoCaching.Online(e.Data.FromId, e.Connection, ClientConnectTypes.P2P));

            //新通道
            punchHoleMessengerSender.OnTunnel.Sub((e) =>
            {
                _ = NewBind(e.ServerType, e.TunnelName);
            });

            //中继连线
            punchHoleMessengerSender.OnRelay.Sub((param) =>
            {
                if (clientInfoCaching.Get(param.Raw.Data.FromId, out ClientInfo client))
                {
                    IConnection connection = null;
                    switch (param.Relay.ServerType)
                    {
                        case ServerType.TCP:
                            connection = registerState.TcpConnection.Clone();
                            break;
                        case ServerType.UDP:
                            connection = registerState.UdpConnection.Clone();
                            break;
                        default:
                            break;
                    }
                    if (connection != null)
                    {
                        connection.Relay = registerState.RemoteInfo.Relay;
                        clientInfoCaching.Online(param.Raw.Data.FromId, connection, ClientConnectTypes.Relay);
                    }
                }
            });

            this.punchHoleMessengerSender = punchHoleMessengerSender;
            //有人要求反向链接
            punchHoleMessengerSender.OnReverse.Sub(OnReverse);
            //本客户端注册状态
            registerState.OnRegisterStateChange.Sub(OnRegisterStateChange);
            //收到来自服务器的 在线客户端 数据
            clientsMessengerSender.OnServerClientsData.Sub(OnServerSendClients);

            Logger.Instance.Info("获取外网距离ing...");
            registerState.LocalInfo.RouteLevel = NetworkHelper.GetRouteLevel();
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
                bool udp = false, tcp = false;
                if (config.Client.UseUdp && info.Udp && info.UdpConnecting == false && info.UdpConnected == false)
                {
                    udp = await ConnectUdp(info).ConfigureAwait(false);
                    if (udp == false)
                    {
                        ulong tunnelName = await NewBind(ServerType.UDP);
                        await punchHoleMessengerSender.SendTunnel(info.Id, tunnelName, ServerType.UDP);
                        await Task.Delay(1000);
                        udp = await ConnectUdp(info, tunnelName).ConfigureAwait(false);
                    }
                }
                if (config.Client.UseTcp && info.Tcp && info.TcpConnecting == false && info.TcpConnected == false)
                {
                    tcp = await ConnectTcp(info).ConfigureAwait(false);
                    if (tcp == false)
                    {
                        ulong tunnelName = await NewBind(ServerType.TCP);
                        await punchHoleMessengerSender.SendTunnel(info.Id, tunnelName, ServerType.TCP);
                        await Task.Delay(1000);
                        tcp = await ConnectTcp(info, tunnelName).ConfigureAwait(false);
                    }
                }

                if ((udp == false || tcp == false) && tryreverse < TryReverseMaxValue)
                {
                    ConnectReverse(info.Id, tryreverse);
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

        private async Task<bool> ConnectUdp(ClientInfo info, ulong tunnelName = 0)
        {
            clientInfoCaching.Connecting(info.Id, true, ServerType.UDP);
            var result = await punchHoleUdp.Send(new ConnectParams
            {
                Id = info.Id,
                TunnelName = tunnelName,
                TryTimes = 5
            }).ConfigureAwait(false);
            if (result.State)
            {
                return result.State;
            }
            if (registerState.RemoteInfo.Relay)
            {
                IConnection connection = registerState.UdpConnection.Clone();
                connection.Relay = registerState.RemoteInfo.Relay;
                clientInfoCaching.Online(info.Id, connection, ClientConnectTypes.Relay);
                _ = punchHoleMessengerSender.SendRelay(info.Id, ServerType.UDP);
                return true;
            }
            else
            {
                Logger.Instance.Error((result.Result as ConnectFailModel).Msg);
                clientInfoCaching.Offline(info.Id, ServerType.UDP);
            }
            return false;
        }
        private async Task<bool> ConnectTcp(ClientInfo info, ulong tunnelName = 1)
        {
            clientInfoCaching.Connecting(info.Id, true, ServerType.TCP);
            var result = await punchHoleTcp.Send(new ConnectParams
            {
                Id = info.Id,
                TunnelName = tunnelName,
                TryTimes = 5
            }).ConfigureAwait(false);
            if (result.State)
            {
                return result.State;
            }
            if (registerState.RemoteInfo.Relay)
            {
                IConnection connection = registerState.TcpConnection.Clone();
                connection.Relay = registerState.RemoteInfo.Relay;
                clientInfoCaching.Online(info.Id, connection, ClientConnectTypes.Relay);
                _ = punchHoleMessengerSender.SendRelay(info.Id, ServerType.TCP);
                return true;
            }
            else
            {
                Logger.Instance.Error((result.Result as ConnectFailModel).Msg);
                clientInfoCaching.Offline(info.Id, ServerType.TCP);
            }
            return false;
        }

        private void OnRegisterStateChange(bool state)
        {
            firstClients.Reset();
            clientInfoCaching.Clear();
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
                        Udp = item.Udp
                    };
                    clientInfoCaching.Add(client);
                    if (firstClients.Get() && config.Client.AutoPunchHole)
                    {
                        if (registerState.LocalInfo.TcpPort == registerState.RemoteInfo.TcpPort || registerState.LocalInfo.UdpPort == registerState.RemoteInfo.UdpPort)
                        {
                            ConnectClient(client);
                        }
                        else
                        {
                            ConnectReverse(client.Id, TryReverseMinValue);
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

        private async Task<ulong> NewBind(ServerType serverType, ulong tunnelName = 0)
        {
            IPAddress serverAddress = NetworkHelper.GetDomainIp(config.Server.Ip);
            int port = NetworkHelper.GetRandomPort();

            return serverType switch
            {
                ServerType.TCP => await NewBindTcp(port, serverAddress, tunnelName),
                ServerType.UDP => await NewBindUdp(port, serverAddress, tunnelName),
                _ => 0,
            };
        }
        private async Task<ulong> NewBindUdp(int localport, IPAddress serverAddress, ulong tunnelName)
        {
            UdpServer tempUdpServer = new UdpServer();
            tempUdpServer.OnPacket.Sub(udpServer.InputData);
            tempUdpServer.OnDisconnect.Sub((IConnection connection) => tempUdpServer.Stop());
            tempUdpServer.Start(localport, config.Client.BindIp, config.Client.TimeoutDelay);
            IConnection connection = await tempUdpServer.CreateConnection(new IPEndPoint(serverAddress, config.Server.UdpPort));

            int port = await registerMessengerSender.GetTunnelPort(connection);
            tunnelName = await registerMessengerSender.AddTunnel(registerState.UdpConnection, tunnelName, port, localport);
            return tunnelName;
        }
        private async Task<ulong> NewBindTcp(int localport, IPAddress serverAddress, ulong tunnelName)
        {
            TcpServer tempTcpServer = new TcpServer();
            tempTcpServer.SetBufferSize(config.Client.TcpBufferSize);
            tempTcpServer.OnPacket.Sub(tcpServer.InputData);
            tempTcpServer.OnDisconnect.Sub((IConnection connection) => tempTcpServer.Stop());
            tempTcpServer.Start(localport, config.Client.BindIp);

            IPEndPoint bindEndpoint = new IPEndPoint(config.Client.BindIp, localport);

            Socket tcpSocket = new(bindEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            tcpSocket.KeepAlive(time: config.Client.TimeoutDelay / 5 / 1000);
            tcpSocket.ReuseBind(bindEndpoint);
            tcpSocket.Connect(new IPEndPoint(serverAddress, config.Server.TcpPort));

            IPAddress localAddress = (tcpSocket.LocalEndPoint as IPEndPoint).Address;

            IConnection connection = tcpServer.BindReceive(tcpSocket, config.Client.TcpBufferSize);

            int port = await registerMessengerSender.GetTunnelPort(connection);
            tunnelName = await registerMessengerSender.AddTunnel(registerState.TcpConnection, tunnelName, port, localport);
            return tunnelName;
        }
    }
}
