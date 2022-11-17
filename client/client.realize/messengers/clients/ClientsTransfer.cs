using client.messengers.clients;
using client.messengers.punchHole;
using client.messengers.punchHole.tcp;
using client.messengers.punchHole.udp;
using client.messengers.register;
using client.messengers.relay;
using client.realize.messengers.heart;
using client.realize.messengers.punchHole;
using client.realize.messengers.relay;
using common.libs;
using common.server;
using common.server.model;
using common.server.servers.rudp;
using System;
using System.Collections.Concurrent;
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
        private readonly HeartMessengerSender heartMessengerSender;
        private readonly RelayMessengerSender relayMessengerSender;
        private readonly IConnecRouteCaching connecRouteCaching;

        private const byte TryReverseMaxValue = 2;
        private object lockObject = new();

        public ClientsTransfer(ClientsMessengerSender clientsMessengerSender,
            IPunchHoleUdp punchHoleUdp, IPunchHoleTcp punchHoleTcp, IClientInfoCaching clientInfoCaching,
            RegisterStateInfo registerState, PunchHoleMessengerSender punchHoleMessengerSender, Config config,
            IUdpServer udpServer, ITcpServer tcpServer, HeartMessengerSender heartMessengerSender,
            RelayMessengerSender relayMessengerSender, IClientsTunnel clientsTunnel, IConnecRouteCaching connecRouteCaching
        )
        {
            this.punchHoleUdp = punchHoleUdp;
            this.punchHoleTcp = punchHoleTcp;
            this.registerState = registerState;
            this.clientInfoCaching = clientInfoCaching;
            this.config = config;
            this.udpServer = udpServer;
            this.heartMessengerSender = heartMessengerSender;
            this.relayMessengerSender = relayMessengerSender;
            this.connecRouteCaching = connecRouteCaching;


            punchHoleUdp.OnStep1Handler.Sub((e) =>
            {
                if (config.Client.UseUdp == true)
                {
                    clientInfoCaching.SetConnecting(e.RawData.FromId, true);
                }
            });
            punchHoleUdp.OnStep2FailHandler.Sub((e) =>
            {
                if (clientInfoCaching.Get(e.RawData.FromId, out ClientInfo client))
                {
                    clientInfoCaching.SetConnecting(e.RawData.FromId, false);
                    if (e.RawData.TunnelName > (ulong)TunnelDefaults.MAX)
                    {
                        clientInfoCaching.RemoveTunnelPort(e.RawData.TunnelName);
                        clientInfoCaching.RemoveUdpserver(e.RawData.TunnelName, true);
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
                    //  clientInfoCaching.RemoveUdpserver(e.RawData.TunnelName);
                    _ = clientsMessengerSender.RemoveTunnel(registerState.OnlineConnection, e.RawData.TunnelName);
                }
            });
            punchHoleUdp.OnStep4Handler.Sub((e) =>
            {
                clientInfoCaching.Online(e.Data.FromId, e.Connection, ClientConnectTypes.P2P);
                if (e.RawData.TunnelName > (ulong)TunnelDefaults.MAX)
                {
                    clientInfoCaching.RemoveTunnelPort(e.RawData.TunnelName);
                    // clientInfoCaching.RemoveUdpserver(e.RawData.TunnelName);
                    _ = clientsMessengerSender.RemoveTunnel(registerState.OnlineConnection, e.RawData.TunnelName);
                }
            });

            punchHoleTcp.OnStep1Handler.Sub((e) =>
            {
                if (config.Client.UseTcp == true)
                {
                    clientInfoCaching.SetConnecting(e.RawData.FromId, true);
                }
            });
            punchHoleTcp.OnStep2FailHandler.Sub((e) =>
            {
                clientInfoCaching.SetConnecting(e.RawData.FromId, false);
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
            clientsTunnel.OnDisConnect = Disconnect;

            //中继连线
            relayMessengerSender.OnRelay.Sub((param) =>
            {
                if (param.RelayIds.Length >= 3)
                {
                    //连接谁
                    ulong id = param.RelayIds[^1];
                    //通过谁连
                    ulong connectid = param.RelayIds[1];
                    if (clientInfoCaching.Get(connectid, out ClientInfo connectionClient))
                    {
                        _ = Relay(connectionClient.Connection, param.RelayIds, false);
                    }
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

        public void Disconnect(IConnection connection, IConnection regConnection)
        {
            if (ReferenceEquals(regConnection, connection))
            {
                return;
            }
            clientInfoCaching.Offline(connection.ConnectId);
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
            if (registerState.LocalInfo.IsConnecting)
            {
                return;
            }
            Task.Run(async () =>
            {
                EnumConnectResult result = await ConnectTcp(client).ConfigureAwait(false);
                if (result == EnumConnectResult.Fail)
                {
                    result = await ConnectUdp(client).ConfigureAwait(false);
                }

                if (result == EnumConnectResult.Fail)
                {
                    if (client.TryReverseValue < TryReverseMaxValue)
                    {
                        client.TryReverseValue++;
                        ConnectReverse(client);
                    }
                    else
                    {
                        _ = Relay(client, true);
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
            if (client.Connected)
            {
                return EnumConnectResult.Success;
            }
            if (client.Connecting)
            {
                return EnumConnectResult.BreakOff;
            }
            if ((config.Client.UseUdp & client.UseUdp) == false)
            {
                return EnumConnectResult.BreakOff;
            }

            clientInfoCaching.SetConnecting(client.Id, true);

            ulong[] tunnelNames = new ulong[] { (ulong)TunnelDefaults.UDP, (ulong)TunnelDefaults.MIN };
            if (config.Client.UseOriginPort == false)
            {
                tunnelNames[0] = (ulong)TunnelDefaults.MIN;
            }

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
                    return EnumConnectResult.Success;
                }
                Logger.Instance.Error((result.Result as ConnectFailModel).Msg);
            }

            clientInfoCaching.Offline(client.Id);
            return EnumConnectResult.Fail;
        }

        private async Task<EnumConnectResult> ConnectTcp(ClientInfo client)
        {
            if (client.Connected)
            {
                return EnumConnectResult.Success;
            }
            if (client.Connecting)
            {
                return EnumConnectResult.BreakOff;
            }
            if ((config.Client.UseTcp & client.UseTcp) == false)
            {
                return EnumConnectResult.BreakOff;
            }

            clientInfoCaching.SetConnecting(client.Id, true);

            ulong[] tunnelNames = new ulong[] { (ulong)TunnelDefaults.TCP, (ulong)TunnelDefaults.MIN };
            if (config.Client.UseOriginPort == false)
            {
                tunnelNames[0] = (ulong)TunnelDefaults.MIN;
            }
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
                    return EnumConnectResult.Success;
                }
                Logger.Instance.Error((result.Result as ConnectFailModel).Msg);
            }

            clientInfoCaching.Offline(client.Id);
            return EnumConnectResult.Fail;
        }

        public async Task Ping()
        {
            foreach (var item in clientInfoCaching.All())
            {
                try
                {
                    var start = DateTime.Now;
                    var res = await heartMessengerSender.Heart(item.Connection);
                    if (res)
                    {
                        item.Connection.RoundTripTime = (ushort)(DateTime.Now - start).TotalMilliseconds;
                    }
                    else
                    {
                        item.Connection.RoundTripTime = -1;
                    }
                }
                catch (Exception)
                {
                }
            }
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

                    foreach (ClientsClientInfo item in upLineClients)
                    {
                        EnumClientAccess enumClientAccess = (EnumClientAccess)item.ClientAccess;
                        ClientInfo client = new ClientInfo
                        {
                            Id = item.Id,
                            Name = item.Name,
                            UseTcp = (enumClientAccess & EnumClientAccess.UseTcp) == EnumClientAccess.UseTcp,
                            UseUdp = (enumClientAccess & EnumClientAccess.UseUdp) == EnumClientAccess.UseUdp,
                            UsePunchHole = (enumClientAccess & EnumClientAccess.UsePunchHole) == EnumClientAccess.UsePunchHole,
                            UseRelay = (enumClientAccess & EnumClientAccess.UseRelay) == EnumClientAccess.UseRelay,
                        };
                        clientInfoCaching.Add(client);
                        if (firstClients.IsDefault)
                        {
                            if (config.Client.UsePunchHole && client.UsePunchHole)
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
                            else
                            {
                                _ = Relay(client, true);
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

        public async Task<ConcurrentDictionary<ulong, ulong[]>> Connects()
        {
            await relayMessengerSender.AskConnects();
            return connecRouteCaching.Connects;
        }
        public async Task<Dictionary<int, int>> Delay(Dictionary<int, ulong[]> paths)
        {
            Dictionary<int, int> data = new Dictionary<int, int>();
            DateTime current;

            foreach (var item in paths)
            {
                data.Add(item.Key, -1);

                IConnection connection = null;
                if (item.Value[1] == 0)
                {
                    connection = registerState.OnlineConnection;
                }
                else
                {
                    if (clientInfoCaching.Get(item.Value[1], out ClientInfo client))
                    {
                        connection = client.Connection;
                    }
                }
                if (connection == null || connection.Connected == false)
                {
                    continue;
                }


                current = DateTime.Now;
                bool res = res = await relayMessengerSender.Delay(item.Value, connection);
                if (res)
                {
                    data[item.Key] = (int)(DateTime.Now - current).TotalMilliseconds;
                }
            }
            return data;
        }

        private async Task Relay(ClientInfo client, bool notify = false)
        {
            if (registerState.RemoteInfo.Relay == false)
            {
                return;
            }

            IConnection connection = registerState.OnlineConnection;
            if (client.Connection == null || client.Connection.Connected == true || connection.Connected == false)
            {
                return;
            }
            await Relay(connection, new ulong[] { registerState.ConnectId, 0, client.Id }, notify);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetId">连接谁</param>
        /// <param name="sourceConnection">通过谁连</param>
        /// <param name="relayids">中继路径</param>
        /// <param name="notify">是否通知目标端</param>
        /// <returns></returns>
        public async Task Relay(IConnection sourceConnection, ulong[] relayids, bool notify = false)
        {
            if (sourceConnection == null || sourceConnection.Connected == false) return;

            IConnection connection = sourceConnection.Clone();
            connection.Relay = true;
            connection.RelayId = relayids;

            ClientConnectTypes relayType = relayids[1] == 0 ? ClientConnectTypes.RelayServer : ClientConnectTypes.RelayNode;
            clientInfoCaching.Online(relayids[^1], connection, relayType);

            if (notify)
            {
                await relayMessengerSender.Relay(relayids, connection);
            }
        }

        [Flags]
        enum EnumConnectResult : byte
        {
            Fail = 1,
            Success = 2,
            BreakOff = 4
        }
    }

}
