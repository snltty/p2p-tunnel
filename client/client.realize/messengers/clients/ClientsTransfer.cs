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

        private readonly ClientsMessengerSender clientsMessengerSender;
        private readonly IPunchHoleUdp punchHoleUdp;
        private readonly IPunchHoleTcp punchHoleTcp;
        private readonly RegisterStateInfo registerState;
        private readonly IClientInfoCaching clientInfoCaching;
        private readonly PunchHoleMessengerSender punchHoleMessengerSender;
        private readonly Config config;
        private readonly IUdpServer udpServer;
        private readonly HeartMessengerSender heartMessengerSender;
        private readonly RelayMessengerSender relayMessengerSender;
        private readonly IClientConnectsCaching connecRouteCaching;

        private object lockObject = new();

        public ClientsTransfer(ClientsMessengerSender clientsMessengerSender,
            IPunchHoleUdp punchHoleUdp, IPunchHoleTcp punchHoleTcp, IClientInfoCaching clientInfoCaching,
            RegisterStateInfo registerState, PunchHoleMessengerSender punchHoleMessengerSender, Config config,
            IUdpServer udpServer, ITcpServer tcpServer, HeartMessengerSender heartMessengerSender,
            RelayMessengerSender relayMessengerSender, IClientsTunnel clientsTunnel, IClientConnectsCaching connecRouteCaching
        )
        {
            this.clientsMessengerSender = clientsMessengerSender;
            this.punchHoleUdp = punchHoleUdp;
            this.punchHoleTcp = punchHoleTcp;
            this.registerState = registerState;
            this.clientInfoCaching = clientInfoCaching;
            this.config = config;
            this.udpServer = udpServer;
            this.heartMessengerSender = heartMessengerSender;
            this.relayMessengerSender = relayMessengerSender;
            this.connecRouteCaching = connecRouteCaching;

            PunchHoleSub();

            //调试注释
            tcpServer.OnDisconnect.Sub((connection) => Disconnect(connection, registerState.TcpConnection));
            udpServer.OnDisconnect.Sub((connection) => Disconnect(connection, registerState.UdpConnection));
            clientsTunnel.OnDisConnect = Disconnect;

            //中继连线
            relayMessengerSender.OnRelay.Sub((param) =>
            {
                _ = Relay(param.Connection, param.RelayIds, false);
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

        private void PunchHoleSub()
        {
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
                    _ = clientsMessengerSender.RemoveTunnel(registerState.OnlineConnection, e.RawData.TunnelName);
                }
            });
            punchHoleUdp.OnStep4Handler.Sub((e) =>
            {
                clientInfoCaching.Online(e.Data.FromId, e.Connection, ClientConnectTypes.P2P);
                if (e.RawData.TunnelName > (ulong)TunnelDefaults.MAX)
                {
                    clientInfoCaching.RemoveTunnelPort(e.RawData.TunnelName);
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
                Logger.Instance.Error($"canot connect you self");
                return;
            }
            if (registerState.LocalInfo.IsConnecting)
            {
                return;
            }
            Task.Run(async () =>
            { 
                /* 两边先试TCP，没成功，再两边都试试UDP
                 *  
                 * TryReverseTcpBit     0b00000010
                 * TryReverseUdpBit     0b00000001
                 * TryReverseTcpUdpBit  0b00000011
                 * TryReverseDefault    0b00000000
                 * 1、A 00 00 -> 00 10 -> B
                 *      A 试了tcp没成功，让B试试
                 * 2、B 00 10 -> 10 00 -> 10 10 -> A
                 *      B 收到反连接请求，先交换保存状态，试试TCP，没成功，继续给A试试，
                 * 3、A 10 10 -> 10 10 -> 10 11 -> B
                 *      A 收到反连接请求，先交换保存状态，前面试过TCP了，试试UDP，没成功，继续让B试试
                 * 4、B 10 11 -> 11 10 -> 11 11
                 *      B 收到反链接请求，先交换保存状态，前面试过TCP了，试试UDP，成就成，没成就全部结束
                 */ 
                EnumConnectResult result = EnumConnectResult.Fail;
                //tcp没试过，先试试tcp
                if ((client.TryReverseValue & ClientInfo.TryReverseTcpBit) == ClientInfo.TryReverseDefault)
                {
                    client.TryReverseValue |= ClientInfo.TryReverseTcpBit;
                    result = await ConnectTcp(client).ConfigureAwait(false);
                }
                //udp没试过，试试udp
                else if ((client.TryReverseValue & ClientInfo.TryReverseUdpBit) == ClientInfo.TryReverseDefault)
                {
                    client.TryReverseValue |= ClientInfo.TryReverseUdpBit;
                    result = await ConnectUdp(client).ConfigureAwait(false);
                }

              
                //没成功
                if (result == EnumConnectResult.Fail)
                {
                    //对面有没试过的，让对面试试
                    if ((client.TryReverseValue >> 2 & ClientInfo.TryReverseTcpUdpBit) != ClientInfo.TryReverseTcpUdpBit)
                    {
                        ConnectReverse(client);
                    }
                    //都试过了， 都不行，中继 
                    else
                    {
                        _ = Relay(client, true); 
                    }
                }
                client.TryReverseValue = ClientInfo.TryReverseDefault;
            });
        }
        //收到反连接请求
        private void OnReverse(OnPunchHoleArg arg)
        {
            if (clientInfoCaching.Get(arg.Data.FromId, out ClientInfo client))
            {
                PunchHoleReverseInfo model = new PunchHoleReverseInfo();
                model.DeBytes(arg.Data.Data);
                //交换状态 , 11 01 -> 01 11
                client.TryReverseValue = (byte)(((model.TryReverse & ClientInfo.TryReverseTcpUdpBit) << 2) | (model.TryReverse >> 2));
                ConnectClient(client);
            }
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
                return EnumConnectResult.Fail;
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
                return EnumConnectResult.Fail;
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
        public async Task<int[]> Delay(ulong[][] paths)
        {
            int[] data = new int[paths.Length];
            DateTime current;
            for (int i = 0; i < paths.Length; i++)
            {
                ulong[] item = paths[i];

                data[i] = -1;

                IConnection connection = null;
                if (item[1] == 0)
                {
                    connection = registerState.OnlineConnection;
                }
                else
                {
                    if (clientInfoCaching.Get(item[1], out ClientInfo client))
                    {
                        connection = client.Connection;
                    }
                }
                if (connection == null || connection.Connected == false)
                {
                    continue;
                }

                current = DateTime.Now;
                bool res = await relayMessengerSender.Delay(item, connection);
                if (res)
                {
                    data[i] = (int)(DateTime.Now - current).TotalMilliseconds;
                }
            }

            return data;
        }

        private async Task Relay(ClientInfo client, bool notify = false)
        {
            if (registerState.RemoteInfo.Relay == false)
            {
                Logger.Instance.Warning($"server relay not available");
                return;
            }

            IConnection connection = registerState.OnlineConnection;
            if (client.Connected == true)
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
            if (relayids.Length < 3)
            {
                Logger.Instance.Error($"relayids length least 3");
                return;
            };
            if (sourceConnection == null || sourceConnection.Connected == false)
            {
                Logger.Instance.Error($"sourceConnection is null");
                return;
            }

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
